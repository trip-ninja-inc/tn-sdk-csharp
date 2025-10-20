using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace TN.SDK.Test;

[TestFixture]
public sealed class TestPrepareDataForGenerateSolutions
{
    private static string CompressAndEncode(object inputData)
    {
        string json = JsonSerializer.Serialize(inputData);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        byte[] compressedBytes = Compress(jsonBytes);

        string base64 = Convert.ToBase64String(compressedBytes);
        return base64;
    }

    private static byte[] Compress(byte[] data)
    {
        using MemoryStream output = new();
        using (ZLibStream zlib = new(output, CompressionLevel.Optimal))
        {
            zlib.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    [Test]
    public void Test_Prepare_Data_For_Generate_Solutions_Valid_Data_Returns_Bytes()
    {
        // Arrange
        var inputData = new { test = "value" };
        string jsonData = JsonSerializer.Serialize(inputData);
        TnApi tnApi = new("", "");

        // Compute Expected result
        string expectedData = CompressAndEncode(inputData);

        // Act
        string encodedData = tnApi.PrepareDataForGenerateSolutions(jsonData);
        bool isDataEqual = expectedData.SequenceEqual(encodedData);

        // Assert
        Assert.That(isDataEqual, Is.True);
    }

    [Test]
    public void Test_Prepare_Data_For_Generate_Solutions_Invalid_Data_Raises_Exception()
    {
        // Arrange
        TnApi tnApi = new("", "");

        // Act & Assert
        TnApiInvalidDataException exception = Assert.Throws<TnApiInvalidDataException>(() => tnApi.PrepareDataForGenerateSolutions(""));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Input must be a valid JSON-encoded string"));
            Assert.That(exception.Code, Is.EqualTo("INVALID_DATA"));
        });
    }
}