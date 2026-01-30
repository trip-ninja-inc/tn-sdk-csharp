using System.Reflection;
using System.Text.Json.Serialization;

namespace TN.SDK.Enums;

/// <summary>
/// Extension to allow easy conversion to JSON value
/// </summary>
internal static class TokenTypeExtensions
{

    /// <summary>
    /// Retrieves the string value from the [JsonPropertyName] attribute.
    /// Usage: TokenType.DataStream.ToJsonValue()
    /// </summary>
    public static string ToJsonValue(this TokenType tokenType)
    {
        MemberInfo memberInfo = tokenType.GetType().GetMember(tokenType.ToString())[0];
        JsonPropertyNameAttribute? attribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();

        return attribute?.Name ?? tokenType.ToString().ToLower();
    }
}
