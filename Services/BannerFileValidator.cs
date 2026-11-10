using Microsoft.AspNetCore.Http;
namespace RaCeDay_Part_2.Services;
public static class BannerFileValidator
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

        public static string? Validate(IFormFile file)
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

}

