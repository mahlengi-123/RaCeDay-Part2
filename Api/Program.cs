using RaceDay.BannerApi;
using RaCeDay_Part_2.Services;

var builder = WebApplication.CreateBuilder(args);
var apiKey = builder.Configuration["BannerApi:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
    throw new InvalidOperationException("Configure BannerApi:ApiKey with at least 32 characters.");

builder.Services.AddControllers();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

var app = builder.Build();
app.UseMiddleware<BannerApiKeyMiddleware>();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "RaceDay Banner API" }));
app.MapControllers();
app.Run();
