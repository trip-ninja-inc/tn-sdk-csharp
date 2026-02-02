

using Microsoft.Extensions.Configuration;

using TN.SDK.Core;

namespace TN.SDK.Test;


[TestFixture]
public sealed class TestConstructor : TnApiTestBase
{
    [Test]
    public void Constructor__ValidSettings__InitializesSuccessfully()
    {
        // Arrange
        TnSdkSettings settings = new()
        {
            ClientId = "valid-id",
            ClientSecret = "valid-secret",
            CredentialFilePath = _tempCredFile,
            ApiUrl = "https://api.tripninja.io"
        };

        // Act & Assert
        Assert.DoesNotThrow(() => new TnApi(settings));
    }

    [TestCase("", "secret", Description = "Empty ClientId")]
    [TestCase("id", "", Description = "Empty ClientSecret")]
    public void Constructor__MissingAuthFieldsInSettings__ThrowsArgumentException(string id, string secret)
    {
        // Arrange
        TnSdkSettings settings = new()
        {
            ClientId = id,
            ClientSecret = secret,
            CredentialFilePath = _tempCredFile
        };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new TnApi(settings));
        Assert.That(ex.Message, Does.Contain("Client ID and Client Secret are required"));
    }

    [TestCase("ftp://invalid-scheme.com")]
    [TestCase("not-a-url")]
    public void Constructor__InvalidApiUrlInSettings__ThrowsArgumentException(string invalidUrl)
    {
        // Arrange
        TnSdkSettings settings = new()
        {
            ClientId = "id",
            ClientSecret = "secret",
            CredentialFilePath = _tempCredFile,
            ApiUrl = invalidUrl
        };

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new TnApi(settings));
        Assert.That(ex.Message, Does.Contain("Invalid API URL"));
    }

    [Test]
    public void Constructor__NonExistentDirectoryInSettings__ThrowsDirectoryNotFoundException()
    {
        // Arrange
        string nonExistentFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string badPath = Path.Combine(nonExistentFolder, "creds.json");

        TnSdkSettings settings = new()
        {
            ClientId = "id",
            ClientSecret = "secret",
            CredentialFilePath = badPath
        };

        // Act & Assert
        _ = Assert.Throws<DirectoryNotFoundException>(() => new TnApi(settings));
    }

    [Test]
    public void Constructor__WithEmptyIConfiguration__ThrowsArgumentException()
    {
        // Arrange
        // Empty config (simulating missing appsettings section)
        IConfiguration config = new ConfigurationBuilder().Build();

        // Act & Assert
        // Should throw because ClientId/Secret will be null after binding
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new TnApi(config));
        Assert.That(ex.Message, Does.Contain("Client ID and Client Secret are required"));
    }
}
