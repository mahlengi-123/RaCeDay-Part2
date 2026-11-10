using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaCeDay_Part_2.Services;

// MVC sends files to the API. Azure credentials and upload operations belong to the API.
public sealed class BannerApiClient : IBlobStorageService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public BannerApiClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public string? Validate(IFormFile file) => BannerFileValidator.Validate(file);

    public async Task<BlobUploadResult> UploadBannerAsync(IFormFile file, CancellationToken ct = default)
    {
        var error = Validate(file);
        if (error != null)
            throw new BannerApiException(error);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/banners");
        AddApiKey(request);
        using var multipart = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        multipart.Add(content, "file", Path.GetFileName(file.FileName));
        request.Content = multipart;

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new BannerApiException("The banner API could not upload your image. Please try again.");

            var result = await response.Content.ReadFromJsonAsync<BlobUploadResult>(cancellationToken: ct);
            if (result == null || string.IsNullOrWhiteSpace(result.BlobName) ||
                !Uri.TryCreate(result.ImageUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
                throw new BannerApiException("The banner API returned an invalid image response.");

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException ||
            (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new BannerApiException("The banner API is unavailable. Please try again shortly.");
        }
    }

    public async Task DeleteAsync(string? blobName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        using var request = new HttpRequestMessage(HttpMethod.Delete,
            "api/banners?blobName=" + Uri.EscapeDataString(blobName));
        AddApiKey(request);
        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new BannerApiException("The previous banner could not be removed from storage.");
        }
        catch (Exception ex) when (ex is HttpRequestException ||
            (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new BannerApiException("The banner API is unavailable for cleanup.");
        }
    }

    private void AddApiKey(HttpRequestMessage request)
    {
        var key = _configuration["BannerApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new BannerApiException("Banner uploads are not configured. Contact the administrator.");
        request.Headers.Add("X-Banner-Api-Key", key);
    }
}
