using System.Net;

using Moq;
using Moq.Protected;

using TN.SDK.Core;
using TN.SDK.Utils;
using TN.SDK.Exceptions;

namespace TN.SDK.Test;
[TestFixture]
public class TestFetchNewCredentialsFromApiAsync : TnApiTestBase
{
    [Test]
    public void FetchNewCredentialsFromApiAsync__ApiReturnsNonSuccess__ThrowsTnAuthenticationFailedException()
    {
        // Arrange
        string errorContent = /*lang=json,strict*/ "{\"error\": \"invalid_client_id\"}";

        // Mock the Auth Endpoint (SdkAuthEndpoint) to return 400 Bad Request
        _ = _httpMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains(Constants.ApiUrls.SDK_AUTH_ENDPOINT)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorContent)
            });

        using TnApi api = GetApiInstance();

        // Act & Assert
        // We expect the custom TnAuthenticationFailedException
        TnAuthenticationFailedException ex = Assert.ThrowsAsync<TnAuthenticationFailedException>(api.FetchNewCredentialsFromApiAsync);

        // Check that the exception message wraps the actual API error
        Assert.That(ex.Message, Does.Contain("Authentication failed"));
        Assert.That(ex.Message, Does.Contain(errorContent));
    }
}
