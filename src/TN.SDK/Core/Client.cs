using System.IO.Compression;
using System.Text;

using TN.SDK.Exceptions;
using TN.SDK.Utils;

namespace TN.SDK.Core;

/// <summary>
/// The entrypoint to the Trip Ninja SDK. Exposes useful functionality of the Trip Ninja API to the end user.
/// </summary>
public class TnApi
{
    private static readonly CompressionLevel ZLIB_DEFAULT_COMPRESSION_LEVEL = Constants.CompressionSettings.DEFAULT_COMPRESSION_LEVEL;

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