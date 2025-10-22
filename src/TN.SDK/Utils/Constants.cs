using System.IO.Compression;

namespace TN.SDK.Utils;

/// <summary>
/// Contains global constants used throughout the SDK.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Configuration settings related to data compression.
    /// </summary>
    public static class CompressionSettings
    {
        /// <summary>
        /// The default compression level used when compressing data.
        /// <param>
        /// This is set to <see cref="CompressionLevel.Optimal"/> to balance compression ratio and speed.
        /// </param>
        /// </summary>
        public const CompressionLevel DEFAULT_COMPRESSION_LEVEL = CompressionLevel.Optimal;
    }
}