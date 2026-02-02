using System.Net;
using System.Text.Json;

using Moq;
using Moq.Protected;

using TN.SDK.Core;


namespace TN.SDK.Test;

/// <summary>
/// Abstract base class containing shared Setup, Teardown, and Helper methods
/// </summary>
public abstract class TnApiTestBase
{
    protected string _tempCredFile = null!;
    protected Mock<HttpMessageHandler> _httpMock = null!;
    protected const string ValidClientId = "test-id";
    protected const string ValidClientSecret = "test-secret";

    [SetUp]
    public void BaseSetup()
    {
        // Create a unique temp file for each test
        _tempCredFile = Path.Combine(Path.GetTempPath(), $"tn_creds_{Guid.NewGuid()}.json");

        // Initialize the HTTP Mock
        _httpMock = new Mock<HttpMessageHandler>();
    }

    [TearDown]
    public void BaseTearDown()
    {
        // Clean up temp file
        if (File.Exists(_tempCredFile))
        {
            try { File.Delete(_tempCredFile); } catch { }
        }

        // Clean up env vars if set in tests
        Environment.SetEnvironmentVariable("TN_SDK_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("TN_SDK_CLIENT_SECRET", null);
    }

    // -- Helpers --

    protected TnApi GetApiInstance(string? urlOverride = null, int timeout = 5)
    {
        TnSdkSettings settings = new()
        {
            ClientId = ValidClientId,
            ClientSecret = ValidClientSecret,
            CredentialFilePath = _tempCredFile,
            ApiUrl = urlOverride ?? "http://api.tripninja.io",
            TimeoutSeconds = timeout
        };
        return new TnApi(
            settings,
            _httpMock.Object
        );
    }

    protected void SetupAuthResponse(string tokenValue)
    {
        Dictionary<string, string> authResponse = new()
        {
            { "prod_token", tokenValue },
            { "sandbox_token", "sbx-" + tokenValue }
        };

        _ = _httpMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("http://api.tripninja.io")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(authResponse))
            });
    }

    protected void SetupEndpointResponse(HttpMethod method, string endpoint, HttpStatusCode status, string jsonResponse)
    {
        _ = _httpMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains(endpoint) &&
                    req.Method == method),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(jsonResponse)
            });
    }

    protected void VerifyRequestCount(string endpointFragment, Times times)
    {
        _httpMock.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(endpointFragment)),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
