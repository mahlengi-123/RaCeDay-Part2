using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.BannerApi;
using RaceDay.BannerApi.Controllers;
using RaCeDay_Part_2.Services;

namespace RaceDay.BannerApi.Tests;

public class BannerApiTests
{
    private const string Key = "unit-test-key-000000000000000000000000";

    [Theory]
    [InlineData("")]
    [InlineData("wrong-key")]
    public async Task Api_RejectsMissingOrWrongKey(string key)
    {
        bool called = false;
        var middleware = new BannerApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, Config());
        var context = Context("/api/banners", key);
        await middleware.InvokeAsync(context);
        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task Api_AcceptsValidKey()
    {
        bool called = false;
        var middleware = new BannerApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, Config());
        await middleware.InvokeAsync(Context("/api/banners", Key));
        Assert.True(called);
    }

    [Fact]
    public async Task Health_DoesNotRequireApiKey()
    {
        bool called = false;
        var middleware = new BannerApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, Config());
        await middleware.InvokeAsync(Context("/health", ""));
        Assert.True(called);
    }

    [Fact]
    public async Task Upload_InvalidImage_DoesNotCallStorage()
    {
        var storage = new FakeStorage { ValidationError = "Invalid image" };
        var controller = Controller(storage);
        var result = await controller.Upload(File(), default);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid image", Assert.IsType<ProblemDetails>(bad.Value).Detail);
        Assert.Equal(0, storage.Uploads);
    }

    [Fact]
    public async Task Upload_ReturnsAzureBlobNameAndUrl()
    {
        var storage = new FakeStorage();
        var result = Assert.IsType<OkObjectResult>(await Controller(storage).Upload(File(), default));
        var uploaded = Assert.IsType<BlobUploadResult>(result.Value);
        Assert.Equal(storage.Result.BlobName, uploaded.BlobName);
        Assert.Equal(storage.Result.ImageUrl, uploaded.ImageUrl);
        Assert.Equal(1, storage.Uploads);
    }

    [Fact]
    public async Task Upload_AzureFailure_Returns503WithoutExceptionDetails()
    {
        var storage = new FakeStorage { FailUpload = true };
        var result = Assert.IsType<ObjectResult>(await Controller(storage).Upload(File(), default));
        Assert.Equal(503, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.DoesNotContain("private", problem.Detail ?? "");
    }

    [Theory]
    [InlineData("../private.png")]
    [InlineData("profiles/someone.png")]
    [InlineData("banners/not-a-guid.png")]
    public async Task Delete_RejectsNamesOutsideGeneratedBanners(string name)
    {
        var storage = new FakeStorage();
        Assert.IsType<BadRequestObjectResult>(await Controller(storage).Delete(name, default));
        Assert.Null(storage.Deleted);
    }

    [Fact]
    public async Task Delete_ValidBanner_CallsStorage()
    {
        var storage = new FakeStorage();
        var name = "banners/" + new string('a', 32) + ".png";
        Assert.IsType<NoContentResult>(await Controller(storage).Delete(name, default));
        Assert.Equal(name, storage.Deleted);
    }

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["BannerApi:ApiKey"] = Key }).Build();

    private static DefaultHttpContext Context(string path, string key)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers["X-Banner-Api-Key"] = key;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static BannersController Controller(FakeStorage storage) => new(storage, NullLogger<BannersController>.Instance)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private static IFormFile File() => new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "file", "banner.png");

    private sealed class FakeStorage : IBlobStorageService
    {
        public string? ValidationError { get; init; }
        public bool FailUpload { get; init; }
        public int Uploads { get; private set; }
        public string? Deleted { get; private set; }
        public BlobUploadResult Result { get; } = new("banners/" + new string('a', 32) + ".png", "https://example.invalid/banner.png");
        public string? Validate(IFormFile file) => ValidationError;
        public Task<BlobUploadResult> UploadBannerAsync(IFormFile file, CancellationToken ct = default)
        {
            Uploads++;
            if (FailUpload) throw new RequestFailedException(503, "private storage error");
            return Task.FromResult(Result);
        }
        public Task DeleteAsync(string? name, CancellationToken ct = default) { Deleted = name; return Task.CompletedTask; }
    }
}
