using System.Text.Json.Serialization;

namespace TN.SDK.Enums;

/// <summary>
/// Enum representing the types of Tokens available
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Token for Production
    /// </summary>
    [JsonPropertyName("prod_token")]
    Production,

    /// <summary>
    /// Token for Sandbox
    /// </summary>
    [JsonPropertyName("sandbox_token")]
    Sandbox,

    /// <summary>
    /// Token for DataStream
    /// </summary>
    [JsonPropertyName("data_stream_token")]
    DataStream
}
