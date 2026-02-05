
using System.IO.Compression;
using System.Text;

using TN.SDK.Core;
using TN.SDK.Exceptions;


namespace TN.SDK.Test;

[TestFixture]
public class TestPrepareDataForGenerateSolutions : TnApiTestBase
{
    [TestCase(/*lang=json,strict*/ "{\"foo\":\"bar\"}")]
    [TestCase("[]")]
    [TestCase("Simple String")]
    public void PrepareDataForGenerateSolutions__ValidJsonString__ReturnsCompressedBase64(string inputJson)
    {
        using TnApi api = GetApiInstance();

        // Act
        string resultBase64 = api.PrepareDataForGenerateSolutions(inputJson);

        // Assert
        // Decode Base64
        byte[] compressedBytes = Convert.FromBase64String(resultBase64);

        // Decompress using ZLib
        using MemoryStream inputMem = new(compressedBytes);
        using ZLibStream zlib = new(inputMem, CompressionMode.Decompress);
        using StreamReader reader = new(zlib, Encoding.UTF8);

        string decompressed = reader.ReadToEnd();

        Assert.That(decompressed, Is.EqualTo(inputJson));
    }

    [Test]
    public void PrepareDataForGenerateSolutions__EmptyInput__ThrowsInvalidDataException()
    {
        using TnApi api = GetApiInstance();
        _ = Assert.Throws<TnApiInvalidDataException>(() => api.PrepareDataForGenerateSolutions(""));
    }
}
