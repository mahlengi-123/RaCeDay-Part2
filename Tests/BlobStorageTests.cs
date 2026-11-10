using RaCeDay_Part_2.Services;
using Microsoft.Extensions.Configuration;

namespace RaceDayy.Tests;

public class BlobStorageTests
{
    [Fact]
    public void UploadResult_PreservesBlobNameAndImageUrl()
    {
        var result = new BlobUploadResult("banners/event.png", "https://example.invalid/event.png");
        Assert.Equal("banners/event.png", result.BlobName);
        Assert.Equal("https://example.invalid/event.png", result.ImageUrl);
    }

    [Fact]
    public void Validate_RejectsAnExecutableRenamedToPng()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true"
        }).Build();
        var service = new BlobStorageService(config);
        using var bytes = new MemoryStream(new byte[] { 0x4D, 0x5A, 0, 0 });
        var file = new FormFile(bytes, 0, bytes.Length, "bannerFile", "renamed.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        Assert.NotNull(service.Validate(file));
    }
}
