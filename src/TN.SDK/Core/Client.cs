using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using TN.SDK.Enums;
using TN.SDK.Exceptions;
using TN.SDK.Utils;

namespace TN.SDK.Core;

/// <summary>
/// The entrypoint to the Trip Ninja SDK. Exposes useful functionality of the Trip Ninja API to the end user.
/// </summary>
public class TnApi : IDisposable
{
    private readonly TnSdkSettings _settings;
    private bool _disposed;
    private readonly HttpClient _httpClient;

    // In-memory cache of credentials
    private Dictionary<string, string> _credentials;

    // Common Options to use for serialization
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    // Handles trailing slash normalization dynamically
    private string ApiUrl => _settings.ApiUrl.TrimEnd('/');

    // Handles path resolution dynamically
    private string CredentialFilePath => Path.GetFullPath(_settings.CredentialFilePath);

    // Safe accessors (Validated in constructor, so we know they aren't empty)
    private string ClientId => _settings.ClientId ?? string.Empty;
    private string ClientSecret => _settings.ClientSecret ?? string.Empty;

    /// <summary>
    /// Initializes the SDK client.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="handler"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public TnApi(TnSdkSettings settings, HttpMessageHandler? handler = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Validate Client ID & Secret
        if (string.IsNullOrEmpty(_settings.ClientId) || string.IsNullOrEmpty(_settings.ClientSecret))
        {
            throw new ArgumentException("Client ID and Client Secret are required.");
        }

        TimeSpan timeoutSeconds = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        // Validate The file path exists
        string? parentDir = Path.GetDirectoryName(_settings.CredentialFilePath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            throw new DirectoryNotFoundException($"Credential file Directory: {parentDir} not found.");
        }

        // Validate the URL
        if (!Uri.TryCreate(_settings.ApiUrl, UriKind.Absolute, out Uri? uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid API URL.");
        }

        // Setup HttpClient
        _httpClient = handler != null
            ? new HttpClient(handler)
            : new HttpClient
            {
                Timeout = timeoutSeconds
            };
        _credentials = LoadTokenFromDisk();
    }

    /// <summary>
    /// Initializes the SDK client.
    /// </summary>
    /// <param name="configuration">IConfiguration data</param>
    /// <param name="section">The Section inside IConfiguration to parse</param>
    /// <param name="handler">Optional HttpMessageHandler</param>
    public TnApi(IConfiguration configuration, string section = "TripNinja", HttpMessageHandler? handler = null) :
    this(BindSettings(configuration, section), handler)
    {

    }

    /// <summary>
    /// Private Method for binding the settings from the configuration
    /// </summary>
    /// <param name="configuration">The Configuration object</param>
    /// <param name="section">The section to Get (defaults to TripNinja)</param>
    /// <returns></returns>
    private static TnSdkSettings BindSettings(IConfiguration configuration, string section = "TripNinja")
    {
        TnSdkSettings settings = new();
        configuration.GetSection(section).Bind(settings);
        return settings;
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

        // Ensure we have credentials loaded at all
        await EnsureCredentialsLoadedAsync();

        string url = $"{ApiUrl}{endpoint}";
        string tokenKey = tokenType.ToJsonValue();
        string currentToken = _credentials.GetValueOrDefault(tokenKey, "");

        // First Attempt
        HttpResponseMessage response = await ExecuteRequestAsync(method, url, currentToken, body, headers);

        // Handle 401 (Unauthorized)
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose(); // Clean up the failed response

            currentToken = await RefreshTokenOn401Async(tokenKey);

            // Retry Request with new token
            response = await ExecuteRequestAsync(method, url, currentToken, body, headers);
        }

        // Final Validation & Parsing
        return await ProcessResponseAsync(response);
    }

    internal async Task<HttpResponseMessage> ExecuteRequestAsync(
        HttpMethod method,
        string url,
        string token,
        object? body,
        Dictionary<string, string>? headers)
    {
        // We pass a Func<HttpRequestMessage> because SendWithNetworkRetriesAsync 
        // needs to create a NEW message object for every retry attempt.
        return await SendWithNetworkRetriesAsync(() =>
            CreateHttpRequestMessage(method, url, token, body, headers)
        );
    }

    internal static HttpRequestMessage CreateHttpRequestMessage(
        HttpMethod method,
        string url,
        string token,
        object? body,
        Dictionary<string, string>? headers)
    {
        HttpRequestMessage request = new(method, url);
        _ = request.Headers.TryAddWithoutValidation("Authorization", $"Token {token}");

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> kvp in headers)
            {
                _ = request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }

        if (body != null)
        {
            request.Content = body is HttpContent content ? content : JsonContent.Create(body);
        }

        return request;
    }

    internal async Task<string> RefreshTokenOn401Async(string tokenKey)
    {
        // Check disk to get latest credentials
        Dictionary<string, string> diskTokenData = LoadTokenFromDisk();

        if (IsDiskTokenNewer(diskTokenData))
        {
            _credentials = diskTokenData;
        }
        else
        {
            // If disk is also stale, call the API
            _credentials = await FetchNewCredentialsFromApiAsync();
        }

        return _credentials.GetValueOrDefault(tokenKey, "");
    }

    internal async Task EnsureCredentialsLoadedAsync()
    {
        if (_credentials == null || _credentials.Count == 0)
        {
            _credentials = await FetchNewCredentialsFromApiAsync();
        }
    }

    internal bool IsDiskTokenNewer(Dictionary<string, string> diskData)
    {
        if (diskData.Count == 0)
        {
            return false;
        }

        // If counts differ, it's definitely different (and presumably newer/valid)
        if (diskData.Count != _credentials.Count)
        {
            return true;
        }

        // Check if values differ
        foreach (KeyValuePair<string, string> kvp in diskData)
        {
            if (!_credentials.TryGetValue(kvp.Key, out string? val) || val != kvp.Value)
            {
                return true;
            }
        }
        return false;
    }

    internal static async Task<JsonElement> ProcessResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            response.Dispose();
            throw new HttpRequestException($"Request failed: {response.StatusCode}. {errorContent}");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        response.Dispose();

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
        string url = $"{ApiUrl}{_settings.AuthEndpoint}";

        using HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Headers.Add("X-Client-ID", ClientId);
        request.Headers.Add("X-Client-Secret", ClientSecret);

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
        if (!File.Exists(CredentialFilePath))
        {
            return [];
        }

        try
        {
            string text = File.ReadAllText(CredentialFilePath, Encoding.UTF8);
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
        string tempPath = CredentialFilePath + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(tokenData, JsonOptions);
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            // Atomic move/replace
            File.Move(tempPath, CredentialFilePath, overwrite: true);
        }
        catch (Exception)
        {
            // Skip saving if permissions fail, keep in memory
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch
                {
                    // If we can't delete it, then just continue on
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _httpClient?.Dispose();
        }

        _disposed = true;
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
