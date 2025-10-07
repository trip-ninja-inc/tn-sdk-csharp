namespace TN.SDK;

/// <summary>
/// Base class for all Trip Ninja SDK exceptions.
/// </summary>
/// <param name="message">Human-readable error message</param>
/// <param name="code">Optional error code (similar to API error codes)</param>
public class TnApiException(
    string message = TnApiException.DEFAULT_MESSAGE,
    string code = TnApiException.DEFAULT_CODE
    ) : Exception(message)
{
    private const string DEFAULT_MESSAGE = "Fallback error message for Trip Ninja SDK";
    private const string DEFAULT_CODE = "SDK_ERROR";

    public string Code { get; } = code;

    public override string ToString()
    {
        return $"[{Code}] {Message}";
    }
}


/// <summary>
/// Raised when invalid or malformed data is passed to an SDK function.
/// </summary>
/// <param name="message">Human-readable error message</param>
/// <param name="code">Optional error code (similar to API error codes)</param>
public class TnApiInvalidDataException(
    string message = TnApiInvalidDataException.DEFAULT_MESSAGE,
    string code = TnApiInvalidDataException.DEFAULT_CODE)
    : TnApiException(message, code)
{

    private const string DEFAULT_MESSAGE = "Invalid or malformed data";
    private const string DEFAULT_CODE = "INVALID_DATA";

}
