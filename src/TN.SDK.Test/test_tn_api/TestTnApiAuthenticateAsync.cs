using TN.SDK.Core;

namespace TN.SDK.Test;

[TestFixture]
public class TestAuthenticateAsync : TnApiTestBase
{
    [Test]
    public async Task AuthenticateAsync__Called__FetchesTokenAndSavesToDisk()
    {
        // Arrange
        SetupAuthResponse("manual-auth-token");
        using TnApi api = GetApiInstance();

        // Act
        await api.AuthenticateAsync();

        // Assert
        Assert.That(File.Exists(_tempCredFile));
        string content = File.ReadAllText(_tempCredFile);
        Assert.That(content, Does.Contain("manual-auth-token"));
    }
}
