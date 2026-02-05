
using System.Net;
using System.Text.Json;

using Moq;
using Moq.Protected;

using TN.SDK.Core;
using TN.SDK.Enums;
using TN.SDK.Test;

namespace TN.SDK.Tests;

[TestFixture]
public class TestRequestAsync : TnApiTestBase
{
    [Test]
    public async Task RequestAsync__NoLocalToken__FetchesNewTokenAndCompletesRequest()
    {
        // Arrange
        string expectedToken = "new-token-123";
        SetupAuthResponse(expectedToken);
        SetupEndpointResponse(HttpMethod.Get, "/test", HttpStatusCode.OK, /*lang=json,strict*/ "{\"result\":\"success\"}");

        using TnApi api = GetApiInstance();

        // Act
        JsonElement result = await api.RequestAsync(HttpMethod.Get, "/test");

        // Assert
        Assert.That(result.GetProperty("result").GetString(), Is.EqualTo("success"));

        // VERIFY: The request was sent with the proper token
        _httpMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/test") &&
                req.Headers.Authorization != null &&
                req.Headers.Authorization.ToString() == $"Token {expectedToken}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public async Task RequestAsync__TokenExpired__RefreshesTokenAndRetries()
    {
        // Arrange
        string oldToken = "old-token";
        string newToken = "fresh-token";

        Dictionary<string, string> initialTokenData = new() { { "prod_token", oldToken } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(initialTokenData));

        using TnApi api = GetApiInstance();

        // Mock Sequence
        _ = _httpMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)) // Call 1 (Old Token)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)            // Call 2 (Auth)
            {
                Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "prod_token", newToken }
                }))
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)            // Call 3 (Retry with New Token)
            {
                Content = new StringContent(/*lang=json,strict*/ "{\"data\":\"final-success\"}")
            });

        // Act
        JsonElement result = await api.RequestAsync(HttpMethod.Get, "/test");

        // Assert
        Assert.That(result.GetProperty("data").GetString(), Is.EqualTo("final-success"));

        // Verify the Initial Failed Call used the OLD token
        _httpMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/test") &&
                req.Headers.Authorization!.ToString() == $"Token {oldToken}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        // Verify the Retry Call used the NEW token
        _httpMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/test") &&
                req.Headers.Authorization!.ToString() == $"Token {newToken}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public async Task RequestAsync__MemoryTokenStaleAndDiskTokenFresh__UsesDiskTokenAndRetries()
    {
        // Arrange
        string memoryToken = "stale-token";
        string diskToken = "fresh-disk-token";

        Dictionary<string, string> staleTokenData = new() { { "prod_token", memoryToken } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(staleTokenData));

        // Load Stale into memory
        using TnApi api = GetApiInstance();

        // Update Disk to Fresh
        Dictionary<string, string> freshTokenData = new() { { "prod_token", diskToken } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(freshTokenData));

        // Mock Sequence
        _ = _httpMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)) // 1. Fail (Stale)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)            // 2. Success (Disk)
            {
                Content = new StringContent(/*lang=json,strict*/ "{\"status\":\"recovered\"}")
            });

        // Act
        JsonElement result = await api.RequestAsync(HttpMethod.Get, "/test");

        // Assert
        Assert.That(result.GetProperty("status").GetString(), Is.EqualTo("recovered"));

        // Verify final successful call used the DISK token
        _httpMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/test") &&
                req.Headers.Authorization!.ToString() == $"Token {diskToken}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [TestCase(500)]
    public async Task RequestAsync__TransientNetworkErrors__RetriesUntilSuccess(int failureCode)
    {
        // Arrange
        string token = "abc";
        Dictionary<string, string> tokenData = new() { { "prod_token", token } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(tokenData));
        using TnApi api = GetApiInstance();

        _ = _httpMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/test")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)failureCode))
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)failureCode))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(/*lang=json,strict*/ "{\"status\":\"ok\"}")
            });

        // Act
        JsonElement result = await api.RequestAsync(HttpMethod.Get, "/test");

        // Assert
        Assert.That(result.GetProperty("status").GetString(), Is.EqualTo("ok"));

        // Verify all attempts used the correct token
        _httpMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/test") &&
                req.Headers.Authorization!.ToString() == $"Token {token}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public async Task RequestAsync__DataStreamTokenType__UsesCorrectTokenFromCredentials()
    {
        // Arrange
        string prodToken = "1234";
        string dataStreamToken = "7890";

        // Create mock credentials file
        Dictionary<string, string> tokenData = new()
        {
            { "prod_token", prodToken },
            { "data_stream_token", dataStreamToken }
        };

        // Write these to the temp file that the API will read
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(tokenData));

        using TnApi api = GetApiInstance();
        SetupEndpointResponse(HttpMethod.Get, "/test", HttpStatusCode.OK, /*lang=json,strict*/ "{\"status\":\"ok\"}");

        // Act
        _ = await api.RequestAsync(HttpMethod.Get, "/test", tokenType: TokenType.DataStream);

        // Assert
        // Verify that the actual HTTP request sent with the correct token
        _httpMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/test") &&
                req.Headers.Authorization != null &&
                req.Headers.Authorization.ToString() == $"Token {dataStreamToken}"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
