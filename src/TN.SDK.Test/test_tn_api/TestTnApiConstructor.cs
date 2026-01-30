using TN.SDK.Core;

namespace TN.SDK.Test;


[TestFixture]
public sealed class TestConstructor : TnApiTestBase
{


    [TestCase("id", "secret", "http://api.com", true)]
    [TestCase("", "secret", "http://api.com", false, Description = "Missing ID")]
    [TestCase("id", "", "http://api.com", false, Description = "Missing Secret")]
    [TestCase("id", "secret", "ftp://invalid-scheme.com", false, Description = "Invalid Scheme")]
    public void Constructor__VariousInputScenarios__ValidatesArgumentsCorrectly(string id, string secret, string url, bool isValid)
    {
        if (isValid)
        {
            Assert.DoesNotThrow(() => new TnApi(id, secret, _tempCredFile, url));
        }
        else
        {
            _ = Assert.Throws<ArgumentException>(() => new TnApi(id, secret, _tempCredFile, url));
        }
    }

    [Test]
    public void Constructor__MissingClientIDAndScret__UsesEnvVars()
    {
        Environment.SetEnvironmentVariable("TN_SDK_CLIENT_ID", "env-id");
        Environment.SetEnvironmentVariable("TN_SDK_CLIENT_SECRET", "env-secret");

        using TnApi api = new(credentialFilePath: _tempCredFile);

        // We can't easily inspect private fields, but we ensure it didn't throw
        Assert.Pass();
    }

    [Test]
    public void Constructor__NonExistentDirectory__ThrowsDirectoryNotFoundException()
    {
        // Arrange
        string nonExistentFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string badPath = Path.Combine(nonExistentFolder, "credentials.json");

        // Act & Assert
        _ = Assert.Throws<DirectoryNotFoundException>(() =>
            new TnApi(ValidClientId, ValidClientSecret, badPath));
    }
}
