namespace TN.SDK.Exceptions;

/// <summary>
/// Base class for all Trip Ninja SDK exceptions.
/// </summary>
public class TnApiException(string? message = null, string? code = null) : Exception(message ?? DefaultMessage)
{
    private const string DefaultMessage = "Fallback error message for Trip Ninja SDK";
    private const string DefaultCode = "SDK_ERROR";

    /// <summary>
    /// Error code (defaults to SDK_ERROR if not provided)
    /// </summary>
    public string Code { get; } = code ?? DefaultCode;
}

/// <summary>
/// Raised when invalid or malformed data is passed to an SDK function.
/// </summary>
public class TnApiInvalidDataException(string? message = null, string? code = null) : TnApiException(message ?? DefaultMessage, code ?? DefaultCode)
{
    private const string DefaultMessage = "Invalid or malformed data";
    private const string DefaultCode = "INVALID_DATA";
}

/// <summary>
/// Raised when authentication failed.
/// </summary>
public class TnAuthenticationFailedException(string? message = null, string? code = null) : TnApiException(message ?? DefaultMessage, code ?? DefaultCode)
{
    private const string DefaultMessage = "Authentication failed";
    private const string DefaultCode = "AUTH_FAILED";
}
