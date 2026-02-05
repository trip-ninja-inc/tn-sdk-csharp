using System.Net.Http.Json;

using TN.SDK.Core;

namespace TN.SDK.Test;

[TestFixture]
public class TestCreateHttpRequestMessage : TnApiTestBase
{
    [Test]
    public void CreateHttpRequestMessage__WithJsonBodyAndCustomHeaders__ConstructsRequestCorrectly()
    {
        // Arrange
        using TnApi api = GetApiInstance();
        string token = "test-token";
        string url = "http://foobar.com/";
        Dictionary<string, string> headers = new() { { "X-Custom", "Foo" } };
        var body = new { id = 1 };

        // Act
        HttpRequestMessage request = TnApi.CreateHttpRequestMessage(
            HttpMethod.Post,
            url,
            token,
            body,
            headers
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri!.ToString(), Is.EqualTo(url));

            // Check Auth
            Assert.That(request.Headers.Authorization!.ToString(), Is.EqualTo("Token test-token"));

            // Check Custom Header
            Assert.That(request.Headers.Contains("X-Custom"), Is.True);

            // Check Body Content Type
            Assert.That(request.Content, Is.InstanceOf<JsonContent>());
            Assert.That(request.Content!.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
        });
    }

    [Test]
    public async Task CreateHttpRequestMessage__WithRawStringContent__PreservesContentType()
    {
        // Arrange
        using TnApi api = GetApiInstance();
        StringContent rawContent = new("raw-data");

        // Act
        HttpRequestMessage request = TnApi.CreateHttpRequestMessage(
            HttpMethod.Put, "http://foobar.com", "tok", rawContent, null
        );

        string text = await request.Content!.ReadAsStringAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(request.Content, Is.InstanceOf<StringContent>());

            Assert.That(text, Is.EqualTo("raw-data"));
        });
    }
}
