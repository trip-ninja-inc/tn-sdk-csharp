using System.Text.Json;

using TN.SDK.Core;

namespace TN.SDK.Test;

[TestFixture]
public class TestIsDiskTokenNewer : TnApiTestBase
{
    [Test]
    public void IsDiskTokenNewer__DiskHasSameDataAsMemory__ReturnsFalse()
    {
        // Arrange
        Dictionary<string, string> data = new() { { "prod", "123" } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(data));

        using TnApi api = GetApiInstance(); // Loads "123" into _credentials

        // Act
        bool result = api.IsDiskTokenNewer(data);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsDiskTokenNewer__DiskHasDifferentValue__ReturnsTrue()
    {
        // Arrange
        Dictionary<string, string> initial = new() { { "prod", "old" } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(initial));

        using TnApi api = GetApiInstance(); // Loads "old" into _credentials

        Dictionary<string, string> newData = new() { { "prod", "new" } };

        // Act
        bool result = api.IsDiskTokenNewer(newData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsDiskTokenNewer__DiskHasDifferentCount__ReturnsTrue()
    {
        // Arrange
        Dictionary<string, string> initial = new() { { "prod", "123" } };
        File.WriteAllText(_tempCredFile, JsonSerializer.Serialize(initial));

        using TnApi api = GetApiInstance();

        Dictionary<string, string> newData = new()
        {
            { "prod", "123" },
            { "sandbox", "456" }
        };

        // Act
        bool result = api.IsDiskTokenNewer(newData);

        // Assert
        Assert.That(result, Is.True);
    }
}
