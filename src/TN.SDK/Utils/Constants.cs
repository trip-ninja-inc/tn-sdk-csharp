using System.IO.Compression;

namespace TN.SDK;

public static class Constants
{
    public static class ApiAurls
    {
        public const string SANDBOX_API_URL = "https://sandbox.tripninja.io";
        public const string PRODUCTION_API_URL = "https://api.tripninja.io";
    }
    public static class CompressionSettings
    {
        public const CompressionLevel DEFAULT_COMPRESSION_LEVEL = CompressionLevel.Optimal;
    }
}
