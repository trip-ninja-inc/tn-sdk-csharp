namespace TN.SDK;

public class TnApiException(
    string message = TnApiException.DEFAULT_MESSAGE,
    string code = TnApiException.DEFAULT_CODE
    ) : Exception
{
    private const string DEFAULT_MESSAGE = "Fallback error message for Trip Ninja SDK";
    private const string DEFAULT_CODE = "SDK_ERROR";

    public string Code { get; } = code;
    public override string Message { get; } = message;

    public override string ToString()
    {
        return $"[{Code}] {Message}";
    }
}


/// <summary>
/// Raised when invalid or malformed data is passed to an SDK function.
/// </summary>
public class TnApiInvalidDataException(
    string message = TnApiInvalidDataException.DEFAULT_MESSAGE,
    string code = TnApiInvalidDataException.DEFAULT_CODE)
    : TnApiException(message, code)
{

    private const string DEFAULT_MESSAGE = "Invalid or malformed data";
    private const string DEFAULT_CODE = "INVALID_DATA";

}