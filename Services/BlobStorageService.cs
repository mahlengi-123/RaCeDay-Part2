
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace RaCeDay_Part_2.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private const long MaxBytes = 2 * 1024 * 1024;

        private static readonly Dictionary<string, string> AllowedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".webp", "image/webp" }
            };

        private readonly BlobContainerClient _container;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureBlob:ConnectionString"]
                ?? configuration["AzureStorage:ConnectionString"];

            var containerName =
                configuration["AzureBlob:ContainerName"]
                ?? "event-banners";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob connection string is not configured.");
            }

            _container = new BlobContainerClient(
                connectionString,
                containerName);
        }

        public string? Validate(IFormFile file) => BannerFileValidator.Validate(file);

        public async Task<BlobUploadResult> UploadBannerAsync(
            IFormFile file,
            CancellationToken ct = default)
        {
            var error = Validate(file);

            if (error != null)
                throw new ArgumentException(error, nameof(file));

            var extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            var blobName =
                $"banners/{Guid.NewGuid():N}{extension}";

            var blob = _container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();

            await blob.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = AllowedTypes[extension],
                        CacheControl = "public, max-age=31536000"
                    }
                },
                ct);

            return new BlobUploadResult(
                blobName,
                blob.Uri.ToString());
        }

        public async Task DeleteAsync(
            string? blobName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                return;

            await _container.DeleteBlobIfExistsAsync(
                blobName,
                cancellationToken: ct);
        }
    }
}
