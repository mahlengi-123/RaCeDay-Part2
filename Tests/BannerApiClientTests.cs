using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using RaCeDay_Part_2.Services;

namespace RaceDayy.Tests;

public class BannerApiClientTests
{
    private const string Key = "test-key-0000000000000000000000000000";
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a4m8AAAAASUVORK5CYII=");

    [Fact]
    public async Task Upload_SendsMultipartFileAndServerApiKey()
    {
        var handler = new Handler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/banners", request.RequestUri!.AbsolutePath);
            Assert.Equal(Key, request.Headers.GetValues("X-Banner-Api-Key").Single());
            Assert.IsType<MultipartFormDataContent>(request.Content);
            var bytes = await request.Content!.ReadAsByteArrayAsync();
            Assert.Contains("banner.png", Encoding.UTF8.GetString(bytes));
            return Json();
        });
        var result = await Client(handler).UploadBannerAsync(File());
        Assert.Equal("banners/test.png", result.BlobName);
        Assert.Equal("https://example.invalid/banner.png", result.ImageUrl);
    }

    [Fact]
    public async Task Upload_RejectsInvalidImageBeforeHttp()
    {
        var handler = new Handler(_ => throw new Exception("HTTP must not be called"));
        var invalid = new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "bannerFile", "bad.png")
        {
            Headers = new HeaderDictionary(), ContentType = "image/png"
        };
        await Assert.ThrowsAsync<BannerApiException>(() => Client(handler).UploadBannerAsync(invalid));
    }

    [Fact]
    public async Task Upload_ServiceFailure_ReturnsFriendlyError()
    {
        var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var ex = await Assert.ThrowsAsync<BannerApiException>(() => Client(handler).UploadBannerAsync(File()));
        Assert.Contains("try again", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_NetworkFailure_ReturnsFriendlyError()
    {
        var handler = new Handler(_ => throw new HttpRequestException("private network detail"));
        var ex = await Assert.ThrowsAsync<BannerApiException>(() => Client(handler).UploadBannerAsync(File()));
        Assert.DoesNotContain("private", ex.Message);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"blobName\":\"banners/test.png\",\"imageUrl\":\"javascript:bad\"}")]
    [InlineData("not-json")]
    public async Task Upload_RejectsInvalidApiResult(string body)
    {
        var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));
        await Assert.ThrowsAsync<BannerApiException>(() => Client(handler).UploadBannerAsync(File()));
    }

    [Fact]
    public async Task Delete_UsesApiAndEscapesBlobName()
    {
        var handler = new Handler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal(Key, request.Headers.GetValues("X-Banner-Api-Key").Single());
            Assert.Contains("blobName=", request.RequestUri!.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        await Client(handler).DeleteAsync("banners/test.png");
    }

    [Fact]
    public async Task Upload_MissingApiKey_DoesNotSendFile()
    {
        var handler = new Handler(_ => throw new Exception("HTTP must not be called"));
        var config = new ConfigurationBuilder().Build();
        var client = new BannerApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5081/") }, config);
        await Assert.ThrowsAsync<BannerApiException>(() => client.UploadBannerAsync(File()));
    }

    private static BannerApiClient Client(Handler handler)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["BannerApi:ApiKey"] = Key }).Build();
        return new BannerApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5081/") }, config);
    }

    private static IFormFile File() => new FormFile(new MemoryStream(Png), 0, Png.Length, "bannerFile", "banner.png")
    {
        Headers = new HeaderDictionary(), ContentType = "image/png"
    };

    private static HttpResponseMessage Json() => new(HttpStatusCode.OK) {
        Content = new StringContent("{\"blobName\":\"banners/test.png\",\"imageUrl\":\"https://example.invalid/banner.png\"}", Encoding.UTF8, "application/json")
    };

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handle(request);
    }
}
