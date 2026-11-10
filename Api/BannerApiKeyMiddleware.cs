using System.Security.Cryptography;
using System.Text;

namespace RaceDay.BannerApi;

public sealed class BannerApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly byte[] _expectedHash;

    public BannerApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var key = configuration["BannerApi:ApiKey"]
            ?? throw new InvalidOperationException("Banner API key is not configured.");
        _expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/banners"))
        {
            var supplied = context.Request.Headers["X-Banner-Api-Key"].ToString();
            if (supplied.Length > 256 || !CryptographicOperations.FixedTimeEquals(
                _expectedHash, SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "A valid banner API key is required." });
                return;
            }
        }

        await _next(context);
    }
}
