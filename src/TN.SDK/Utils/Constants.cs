using System.IO.Compression;

namespace TN.SDK.Utils;

public static class Constants
{
    public static class ApiUrls
    {
        public static string SANDBOX_API_URL => Environment.GetEnvironmentVariable("API_SANDBOX_URL") ?? "https://sandbox.tripninja.io";

        public static string PRODUCTION_API_URL => Environment.GetEnvironmentVariable("API_PRODUCTION_URL") ?? "https://api.tripninja.io";
    }
    public static class CompressionSettings
    {
        public const CompressionLevel DEFAULT_COMPRESSION_LEVEL = CompressionLevel.Optimal;
    }
}