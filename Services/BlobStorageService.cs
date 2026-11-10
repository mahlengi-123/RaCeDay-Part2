
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

        public string? Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "The selected file is empty.";

            if (file.Length > MaxBytes)
                return "The banner must be 2 MB or smaller.";

            var extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedTypes.TryGetValue(
                extension, out var expectedType))
            {
                return "Only JPG, PNG, GIF or WebP images are allowed.";
            }

            if (!string.Equals(
                file.ContentType,
                expectedType,
                StringComparison.OrdinalIgnoreCase))
            {
                return "The image content type is invalid.";
            }

            using var stream = file.OpenReadStream();

            Span<byte> header = stackalloc byte[12];
            int read = stream.Read(header);

            bool valid = expectedType switch
            {
                "image/jpeg" =>
                    read >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                "image/png" =>
                    read >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A,

                "image/gif" =>
                    read >= 6 &&
                    header[0] == 'G' &&
                    header[1] == 'I' &&
                    header[2] == 'F' &&
                    header[3] == '8' &&
                    (header[4] == '7' || header[4] == '9') &&
                    header[5] == 'a',

                "image/webp" =>
                    read >= 12 &&
                    header[0] == 'R' &&
                    header[1] == 'I' &&
                    header[2] == 'F' &&
                    header[3] == 'F' &&
                    header[8] == 'W' &&
                    header[9] == 'E' &&
                    header[10] == 'B' &&
                    header[11] == 'P',

                _ => false
            };

            return valid
                ? null
                : "The file does not appear to be a valid image.";
        }

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
