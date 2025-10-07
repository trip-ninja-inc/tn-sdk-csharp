using System.IO.Compression;
using System.Text;

namespace TN.SDK
{
    /// <summary>
    /// The entrypoint to the Trip Ninja SDK. Exposes useful functionality of the Trip Ninja API to the end user.
    /// </summary>
    public class TnApi
    {
        private static readonly string TN_BASE_PROD_URL = Constants.ApiAurls.PRODUCTION_API_URL;
        private static readonly string TN_BASE_SANDBOX_URL = Constants.ApiAurls.SANDBOX_API_URL;
        private static readonly CompressionLevel GZIP_DEFAULT_COMPRESSION_LEVEL = Constants.CompressionSettings.DEFAULT_COMPRESSION_LEVEL;

        private readonly string _accessToken;
        private readonly string _refreshToken;
        private string BaseUrl { get; }

        /// <summary>
        /// Initializes and instantiates the SDK client.
        /// </summary>
        /// <param name="accessToken">The token provided by the Admin Panel that is used to gain access to the API.</param>
        /// <param name="refreshToken"> The token provided by the Admin Panel that is used to refresh a user's access token.</param>
        /// <param name="isSandbox">Whether the client is connecting to the sandbox or the production environment.</param>
        public TnApi(string accessToken, string refreshToken, bool isSandbox = false)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            BaseUrl = isSandbox ? TN_BASE_SANDBOX_URL : TN_BASE_PROD_URL;
        }

        /// <summary>
        /// Compresses the input JSON string using GZip and encodes it in base64.
        /// </summary>
        /// <param name="jsonData">A JSON-encoded string.</param>
        /// <returns>Compressed and base64-encoded byte array.</returns>
        /// <exception cref="InvalidDataException">Thrown if input is null or empty or not a valid JSON string.</exception>
        public byte[] PrepareDataForGenerateSolutions(string jsonData)
        {
            // Validate jsonData
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                throw new TnApiInvalidDataException("Input must be a valid JSON-encoded string");
            }

            // Encode the jsonData into bytes
            byte[] inputBytes = Encoding.UTF8.GetBytes(jsonData);

            // Compress
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, GZIP_DEFAULT_COMPRESSION_LEVEL))
            {
                gzipStream.Write(inputBytes, 0, inputBytes.Length);
            }

            // Convert back byte array
            byte[] compressedData = outputStream.ToArray();

            // Convert to base64 and return bytes
            return Encoding.UTF8.GetBytes(Convert.ToBase64String(compressedData));
        }
    }
}