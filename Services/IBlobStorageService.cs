
using Microsoft.AspNetCore.Http;

namespace RaCeDay_Part_2.Services
{
    public interface IBlobStorageService
    {
        string? Validate(IFormFile file);

        Task<BlobUploadResult> UploadBannerAsync(
            IFormFile file,
            CancellationToken ct = default);

        Task DeleteAsync(
            string? blobName,
            CancellationToken ct = default);
    }
}
