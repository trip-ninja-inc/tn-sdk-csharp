using System.IO.Compression;
using System.Text;

namespace TN.SDK;

/// <summary>
/// The entrypoint to the Trip Ninja SDK. Exposes useful functionality of the Trip Ninja API to the end user.
/// </summary>
/// <param name="accessToken">The token provided by the Admin Panel that is used to gain access to the API.</param>
/// <param name="refreshToken"> The token provided by the Admin Panel that is used to refresh a user's access token.</param>
/// <param name="isSandbox">Whether the client is connecting to the sandbox or the production environment.</param>
public class TnApi(string accessToken, string refreshToken, bool isSandbox = false)
{
    private static readonly string TN_BASE_PROD_URL = Constants.ApiUrls.PRODUCTION_API_URL;
    private static readonly string TN_BASE_SANDBOX_URL = Constants.ApiUrls.SANDBOX_API_URL;
    private static readonly CompressionLevel ZLIB_DEFAULT_COMPRESSION_LEVEL = Constants.CompressionSettings.DEFAULT_COMPRESSION_LEVEL;

    private readonly string _accessToken = accessToken;
    private readonly string _refreshToken = refreshToken;
    private readonly string _baseUrl = isSandbox ? TN_BASE_SANDBOX_URL : TN_BASE_PROD_URL;

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
            throw new TnApiInvalidDataException("Input must be a valid JSON-encoded string");
        }

        // Encode the jsonData into bytes
        byte[] inputBytes = Encoding.UTF8.GetBytes(jsonData);

        // ZLibStream compression
        using MemoryStream memoryStream = new();
        using (ZLibStream deflateStream = new(memoryStream, ZLIB_DEFAULT_COMPRESSION_LEVEL, true))
        {
            deflateStream.Write(inputBytes, 0, inputBytes.Length);
        }

        // Convert back byte array
        byte[] compressedData = memoryStream.ToArray();

        // Convert to base64 and return string
        string preparedDataString = Convert.ToBase64String(compressedData);

        return preparedDataString;
    }
}