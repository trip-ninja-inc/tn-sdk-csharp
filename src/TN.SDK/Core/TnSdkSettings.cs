namespace TN.SDK.Core;

/// <summary>
/// Settings used inside the SDK
/// </summary>
public class TnSdkSettings
{
    /// <summary>
    /// The base URL for the API to use
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.tripninja.io";

    /// <summary>
    /// The endpoint to authenticate with
    /// </summary>
    public string AuthEndpoint { get; set; } = "/sdk/auth/";

    /// <summary>
    /// The Path to the credentials.json file to read/update
    /// </summary>
    public string CredentialFilePath { get; set; } = "credentials.json";

    /// <summary>
    /// The timeout of requests in seconds (Defaults to 30s)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// The Client ID to use for authentication. If null TnApi will throw
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The Client secret to use for authentication. If null TnApi will throw
    /// </summary>
    public string? ClientSecret { get; set; }
}
