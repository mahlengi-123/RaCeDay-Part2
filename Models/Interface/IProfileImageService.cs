
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace RaCeDay.Models.Interface
{
    public interface IProfilImageService
    {
        Task<string?> SaveAsync(IFormFile file);
        void Delete(string? relativePath);
    }

    public class ProfilImageService : IProfilImageService
    {
        private static readonly string[] Allowed =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private const long MaxBytes = 2 * 1024 * 1024;

        private readonly IWebHostEnvironment _env;

        public ProfilImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!Allowed.Contains(ext) || file.Length > MaxBytes)
                return null;

            var webRoot = _env.WebRootPath
                ?? Path.Combine(_env.ContentRootPath, "wwwroot");

            var folder = Path.Combine(webRoot, "uploads", "profile");

            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using (var fs = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(fs);
            }

            return $"/uploads/profile/{fileName}";
        }

        public void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var webRoot = _env.WebRootPath
                ?? Path.Combine(_env.ContentRootPath, "wwwroot");

            var fileName = Path.GetFileName(relativePath);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var fullPath = Path.Combine(
                webRoot, "uploads", "profile", fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
