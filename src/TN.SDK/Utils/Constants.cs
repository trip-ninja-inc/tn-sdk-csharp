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

    /// <summary>
    /// Constants relating to API Networking
    /// </summary>
    public static class ApiUrls
    {
        /// <summary>
        /// The default Production URL for TripNinja.
        /// </summary>
        public const string PRODUCTION_API_URL = "https://api.tripninja.io";

        /// <summary>
        /// The default Sandbox URL for TripNinja.
        /// </summary>
        public const string SANDBOX_API_URL = "https://sandbox.tripninja.io";

        /// <summary>
        /// The default sdk authentication endpoint.
        /// </summary>
        public const string SDK_AUTH_ENDPOINT = "/sdk/auth/";
    }
}
