using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using TN.SDK.Enums;
using TN.SDK.Exceptions;
using TN.SDK.Utils;

namespace TN.SDK.Core;

/// <summary>
/// The entrypoint to the Trip Ninja SDK. Exposes useful functionality of the Trip Ninja API to the end user.
/// </summary>
public class TnApi : IDisposable
{
    private readonly string _tnApiUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _credentialFilePath;
    private readonly TimeSpan _timeout;
    private readonly HttpClient _httpClient;

    // In-memory cache of credentials
    private Dictionary<string, string> _credentials;

    // Common Options to use for serialization
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes the SDK client.
    /// </summary>
    /// <param name="clientId">API Client ID (defaults to env TN_SDK_CLIENT_ID)</param>
    /// <param name="clientSecret">API Client Secret (defaults to env TN_SDK_CLIENT_SECRET)</param>
    /// <param name="credentialFilePath">Credentials file path (defaults to credentials.json)</param>
    /// <param name="tnApiUrl">Base URL for the API</param>
    /// <param name="timeoutSeconds">Default timeout for network requests in seconds</param>
    /// <param name="handler">Optional HttpMessage Handler</param>
    public TnApi(
        string? clientId = null,
        string? clientSecret = null,
        string credentialFilePath = "credentials.json",
        string tnApiUrl = Constants.APIUrls.PRODUCTION_API_URL,
        int timeoutSeconds = 30,
        HttpMessageHandler? handler = null)
    {
        _tnApiUrl = tnApiUrl.TrimEnd('/');
        _clientId = clientId ?? Environment.GetEnvironmentVariable("TN_SDK_CLIENT_ID") ?? "";
        _clientSecret = clientSecret ?? Environment.GetEnvironmentVariable("TN_SDK_CLIENT_SECRET") ?? "";

        // Resolve full path
        _credentialFilePath = Path.GetFullPath(credentialFilePath);
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);

        // Validate Client ID & Secret
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            throw new ArgumentException("Client ID and Client Secret are required.");
        }

        // Validate The file path exists
        string? parentDir = Path.GetDirectoryName(_credentialFilePath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            throw new DirectoryNotFoundException($"Credential file Directory: {parentDir} not found.");
        }

        // Validate the URL
        if (!Uri.TryCreate(_tnApiUrl, UriKind.Absolute, out Uri? uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid API URL.");
        }

        // Setup HttpClient
        _httpClient = handler != null
            ? new HttpClient(handler)
            : new HttpClient
            {
                Timeout = _timeout
            };

        // Initial load
        _credentials = LoadTokenFromDisk();
    }

    /// <summary>
    /// Central internal request handler. Handles Refreshing token, Setting headers, and Retry logic.
    /// </summary>
    /// <param name="method">Http Method</param>
    /// <param name="endpoint">Endpoint to use</param>
    /// <param name="tokenType">The Type of token to use</param>
    /// <param name="body">Body of request (if any)</param>
    /// <param name="headers">Any additional headers</param>
    /// <returns>The JSON result of the response</returns>
    /// <exception cref="HttpRequestException"></exception>
    internal async Task<JsonElement> RequestAsync(
        HttpMethod method,
        string endpoint,
        TokenType tokenType = TokenType.Production,
        object? body = null,
        Dictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        if (_credentials == null || _credentials.Count == 0)
        {
            _credentials = await FetchNewCredentialsFromApiAsync();
        }

        string url = $"{_tnApiUrl}{endpoint}";

        // Helper to prepare the request message
        HttpRequestMessage CreateRequest(string token)
        {
            HttpRequestMessage request = new(method, url);

            // Add Authorization
            _ = request.Headers.TryAddWithoutValidation("Authorization", $"Token {token}");

            // Add Custom Headers
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    _ = request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }

            // Add Body
            if (body != null)
            {
                // If body is already string/content, use it, otherwise JSON serialize
                request.Content = body is HttpContent content ? content : JsonContent.Create(body);
            }

            return request;
        }

        // Get the correct dictionary key (Convert it to it's JSON value)
        string tokenKey = tokenType.ToJsonValue();
        if (!_credentials.ContainsKey(tokenKey))
        {
            // Fallback or force refresh if specific key missing
            _credentials = await FetchNewCredentialsFromApiAsync();
        }

        string currentToken = _credentials.GetValueOrDefault(tokenKey, "");

        // Execute with Network Retry Logic (for 500s, etc)
        HttpResponseMessage response = await SendWithNetworkRetriesAsync(() => CreateRequest(currentToken));

        // Handle 401 (Token Expired)
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose(); // Clean up failed response

            // Check disk to see if another process updated it
            Dictionary<string, string> diskToken = LoadTokenFromDisk();

            // Simple dictionary equality check (count and content)
            bool isDiskDifferent = diskToken.Count != _credentials.Count;
            if (!isDiskDifferent)
            {
                foreach (KeyValuePair<string, string> kvp in diskToken)
                {
                    if (!_credentials.TryGetValue(kvp.Key, out string? val) || val != kvp.Value)
                    {
                        isDiskDifferent = true;
                        break;
                    }
                }
            }

            _credentials = diskToken.Count > 0 && isDiskDifferent ? diskToken : await FetchNewCredentialsFromApiAsync();

            // Retry with new token
            currentToken = _credentials.GetValueOrDefault(tokenKey, "");
            response = await SendWithNetworkRetriesAsync(() => CreateRequest(currentToken));
        }

        // Final check
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed: {response.StatusCode}. {errorContent}");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(jsonString);
    }

    internal async Task<HttpResponseMessage> SendWithNetworkRetriesAsync(Func<HttpRequestMessage> requestFactory)
    {
        int maxRetries = 3;
        // Status codes that if encountered will be retried.
        int[] statusForcelist = [429, 500, 502, 503, 504];

        for (int i = 0; i <= maxRetries; i++)
        {
            HttpRequestMessage request = requestFactory();
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (i < maxRetries && Array.IndexOf(statusForcelist, (int)response.StatusCode) >= 0)
                {
                    // Backoff factor 0.5 equivalent logic: 0.5s, 1s, 2s...
                    double delay = 0.5 * Math.Pow(2, i);
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    response.Dispose();
                    continue;
                }

                return response;
            }
            catch (HttpRequestException) when (i < maxRetries)
            {
                // Handles network blips
                double delay = 0.5 * Math.Pow(2, i);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        // Should not be reached due to loop logic, but acts as final attempt
        return await _httpClient.SendAsync(requestFactory());
    }

    /// <summary>
    /// Calls the SDK Authentication API to get the latest credentials.
    /// </summary>
    /// <returns>Dictionary with the tokens</returns>
    /// <exception cref="TnAuthenticationFailedException"></exception>
    internal async Task<Dictionary<string, string>> FetchNewCredentialsFromApiAsync()
    {
        string url = $"{_tnApiUrl}{Constants.APIUrls.SDK_AUTH_ENDPOINT}";

        using HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Headers.Add("X-Client-ID", _clientId);
        request.Headers.Add("X-Client-Secret", _clientSecret);

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorTxt = await response.Content.ReadAsStringAsync();
                throw new TnAuthenticationFailedException($"Authentication failed: {errorTxt}");
            }

            Dictionary<string, string>? data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            if (data == null || data.Count == 0)
            {
                throw new TnAuthenticationFailedException("Invalid API Response");
            }

            SaveCredentialsToDisk(data);
            return data;
        }
        catch (Exception ex) when (ex is not TnAuthenticationFailedException)
        {
            throw new TnAuthenticationFailedException("Authentication failed due to network or parsing error");
        }
    }

    private Dictionary<string, string> LoadTokenFromDisk()
    {
        if (!File.Exists(_credentialFilePath))
        {
            return [];
        }

        try
        {
            string text = File.ReadAllText(_credentialFilePath, Encoding.UTF8);
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(text, JsonOptions) ?? [];
            return data;
        }
        catch (Exception)
        {
            // If file is corrupt or unreadable, ignore it
            return [];
        }
    }

    private void SaveCredentialsToDisk(Dictionary<string, string> tokenData)
    {
        string tempPath = _credentialFilePath + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(tokenData, JsonOptions);
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            // Atomic move/replace
            File.Move(tempPath, _credentialFilePath, overwrite: true);
        }
        catch (Exception)
        {
            // Skip saving if permissions fail, keep in memory
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Compresses the input JSON string using GZip and encodes it in base64.
    /// </summary>
    /// <param name="jsonData">A JSON-encoded string.</param>
    /// <returns>Compressed and base64-encoded byte array.</returns>
    /// <exception cref="TnApiInvalidDataException">Thrown if input is null or empty or not a valid JSON string.</exception>
    public string PrepareDataForGenerateSolutions(string jsonData)
    {
        // Validate jsonData
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            throw new TnApiInvalidDataException("Input must be a JSON-encoded string");
        }

        // Encode the jsonData into bytes
        byte[] inputBytes = Encoding.UTF8.GetBytes(jsonData);

        // ZLibStream compression
        using MemoryStream outputStream = new();
        using (ZLibStream zlibStream = new(outputStream, Constants.CompressionSettings.DEFAULT_COMPRESSION_LEVEL))
        {
            zlibStream.Write(inputBytes, 0, inputBytes.Length);
        }

        return Convert.ToBase64String(outputStream.ToArray());
    }

    /// <summary>
    /// Retrieves and stores the latest tokens from the API
    /// </summary>
    public async Task AuthenticateAsync()
    {
        _ = await FetchNewCredentialsFromApiAsync();
    }

}
