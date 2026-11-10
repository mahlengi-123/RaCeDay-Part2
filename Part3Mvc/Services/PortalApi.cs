using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Part3.Mvc.Models;

namespace RaceDay.Part3.Mvc.Services;

public sealed class ApiReply<T>
{
    public bool Ok { get; init; }
    public T? Value { get; init; }
    public string Error { get; init; } = "";
}
public sealed class PortalApi
{
    private readonly IHttpClientFactory _clients;
    private readonly IHttpContextAccessor _context;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PortalApi(IHttpClientFactory clients, IHttpContextAccessor context)
    { _clients = clients; _context = context; }

    private HttpClient Client()
    {
        var client = _clients.CreateClient("RaceDayApi");
        var cookie = _context.HttpContext?.Session.GetString("ApiCookie");
        if (!string.IsNullOrWhiteSpace(cookie))
            client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    private static async Task<ApiReply<T>> Parse<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(json))
            return new ApiReply<T> { Ok = true };
        try
        {
            if (response.IsSuccessStatusCode)
                return new ApiReply<T> { Ok = true, Value = JsonSerializer.Deserialize<T>(json, JsonOptions) };
            var parsed = JsonSerializer.Deserialize<ApiMessage>(json, JsonOptions);
            return new ApiReply<T> { Error = parsed?.Message ?? "The request could not be completed." };
        }
        catch (JsonException)
        {
            return new ApiReply<T> { Error = response.IsSuccessStatusCode
                ? "Unexpected response from RaceDay API." : "The request could not be completed." };
        }
    }

    public async Task<ApiReply<T>> Get<T>(string path)
    {
        try
        {
            using var client = Client();
            using var response = await client.GetAsync(path);
            return await Parse<T>(response);
        }
        catch (HttpRequestException) { return new ApiReply<T> { Error = "Cannot connect to the Part 2 API." }; }
        catch (TaskCanceledException) { return new ApiReply<T> { Error = "The API request timed out." }; }
    }

    public async Task<ApiReply<T>> Send<T>(HttpMethod method, string path, object? body = null)
    {
        try
        {
            using var client = Client();
            using var request = new HttpRequestMessage(method, path);
            if (body != null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request);
            return await Parse<T>(response);
        }
        catch (HttpRequestException) { return new ApiReply<T> { Error = "Cannot connect to the Part 2 API." }; }
        catch (TaskCanceledException) { return new ApiReply<T> { Error = "The API request timed out." }; }
    }

    public async Task<(ApiReply<AuthResult> Result, string? Cookie)> Login(AuthInput input)
    {
        try
        {
            using var client = Client();
            using var response = await client.PostAsJsonAsync("api/portal/auth/login", input);
            var result = await Parse<AuthResult>(response);
            string? cookie = null;
            if (result.Ok && response.Headers.TryGetValues("Set-Cookie", out var cookies))
                cookie = cookies.Select(c => c.Split(';', 2)[0])
                    .FirstOrDefault(c => c.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal));
            if (result.Ok && string.IsNullOrWhiteSpace(cookie))
                return (new ApiReply<AuthResult> { Error = "API did not return an authentication cookie." }, null);
            return (result, cookie);
        }
        catch (HttpRequestException) { return (new ApiReply<AuthResult> { Error = "Cannot connect to the Part 2 API." }, null); }
        catch (TaskCanceledException) { return (new ApiReply<AuthResult> { Error = "API request timed out." }, null); }
    }

    public async Task<ApiReply<ApiMessage>> Upload(string path, IFormFile file)
    {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024 ||
            !new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
            return new ApiReply<ApiMessage> { Error = "Choose a JPG, PNG or WebP under 5 MB." };
        try
        {
            using var client = Client();
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var bytes = new StreamContent(stream);
            bytes.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(bytes, "file", Path.GetFileName(file.FileName));
            using var response = await client.PostAsync(path, form);
            return await Parse<ApiMessage>(response);
        }
        catch (HttpRequestException) { return new ApiReply<ApiMessage> { Error = "Cannot connect to image storage through the API." }; }
        catch (TaskCanceledException) { return new ApiReply<ApiMessage> { Error = "Image upload timed out." }; }
    }
}
