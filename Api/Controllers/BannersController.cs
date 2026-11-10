using Azure;
using Microsoft.AspNetCore.Mvc;
using RaCeDay_Part_2.Services;
using System.Text.RegularExpressions;

namespace RaceDay.BannerApi.Controllers;

[ApiController]
[Route("api/banners")]
public sealed class BannersController : ControllerBase
{
    private readonly IBlobStorageService _storage;
    private readonly ILogger<BannersController> _logger;

    public BannersController(IBlobStorageService storage, ILogger<BannersController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        var error = _storage.Validate(file);
        if (error != null)
            return BadRequest(new ProblemDetails { Title = "Invalid banner", Detail = error, Status = 400 });

        try
        {
            var uploaded = await _storage.UploadBannerAsync(file, ct);
            _logger.LogInformation("Banner uploaded through the API: {BlobName}", uploaded.BlobName);
            return Ok(uploaded);
        }
        catch (Exception ex) when (ex is RequestFailedException or HttpRequestException)
        {
            _logger.LogError(ex, "Azure banner upload failed.");
            return Problem(statusCode: 503, title: "Banner storage unavailable",
                detail: "The image could not be uploaded. Please try again.");
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string blobName, CancellationToken ct)
    {
        if (!Regex.IsMatch(blobName, @"^banners/[a-f0-9]{32}\.(jpg|jpeg|png|gif|webp)$"))
            return BadRequest(new ProblemDetails { Title = "Invalid blob name", Status = 400 });

        try
        {
            await _storage.DeleteAsync(blobName, ct);
            _logger.LogInformation("Banner deleted through the API: {BlobName}", blobName);
            return NoContent();
        }
        catch (Exception ex) when (ex is RequestFailedException or HttpRequestException)
        {
            _logger.LogError(ex, "Azure banner deletion failed.");
            return Problem(statusCode: 503, title: "Banner storage unavailable");
        }
    }
}
