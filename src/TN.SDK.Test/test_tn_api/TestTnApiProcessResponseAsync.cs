using System.Net;
using System.Text.Json;

using TN.SDK.Core;

namespace TN.SDK.Test;

[TestFixture]
public class TestProcessResponseAsync : TnApiTestBase
{
    [Test]
    public async Task ProcessResponseAsync__SuccessStatusCode__ReturnsParsedJsonElement()
    {
        // Arrange
        string json = /*lang=json,strict*/ "{\"key\": \"value\"}";
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        // Act
        JsonElement result = await TnApi.ProcessResponseAsync(response);

        // Assert
        Assert.That(result.GetProperty("key").GetString(), Is.EqualTo("value"));
    }

    [Test]
    public void ProcessResponseAsync__FailureStatusCode__ThrowsExceptionWithContent()
    {
        // Arrange
        string errorJson = /*lang=json,strict*/ "{\"error\": \"failed\"}";
        HttpResponseMessage response = new(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorJson)
        };

        // Act & Assert
        HttpRequestException ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await TnApi.ProcessResponseAsync(response));

        Assert.That(ex.Message, Does.Contain("failed"));
        Assert.That(ex.Message, Does.Contain("failed"));
    }
}
