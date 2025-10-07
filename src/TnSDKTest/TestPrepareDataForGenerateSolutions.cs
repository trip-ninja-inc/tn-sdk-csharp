using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace TN.SDK.Test;

[TestClass]
public sealed class TestPrepareDataForGenerateSolutions
{

    private static byte[] CompressAndEncode(object inputData)
    {
        
        string json = JsonSerializer.Serialize(inputData);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        
        byte[] compressedBytes = Compress(jsonBytes);

        
        string base64 = Convert.ToBase64String(compressedBytes);
        return Encoding.UTF8.GetBytes(base64);
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }


    [TestMethod]
    public void Test_Prepare_Data_For_Generate_Solutions_Valid_Data_Returns_Bytes()
    {
        // Arrange
        var inputData = new { test = "value" };
        string jsonData = JsonSerializer.Serialize(inputData);
        TnApi tnApi = new("", "");

        // Compute Expected result
        byte[] expectedData = CompressAndEncode(inputData);


        // Act
        byte[] encodedData = tnApi.PrepareDataForGenerateSolutions(jsonData);

        bool isDataEqual = expectedData.SequenceEqual(encodedData);

        // Assert
        Assert.IsTrue(isDataEqual);
    }

    [TestMethod]
    public void Test_Prepare_Data_For_Generate_Solutions_Invalid_Data_Raises_Exception()
    {
        // Arrange
        TnApi tnApi = new("", "");

        // Assert
        Assert.ThrowsException<TnApiInvalidDataException>(() => tnApi.PrepareDataForGenerateSolutions(""));
    }
}
