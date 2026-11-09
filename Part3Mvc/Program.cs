using RaceDay.Part3.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Portal.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(45);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5176/");
    client.Timeout = TimeSpan.FromSeconds(20);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});
builder.Services.AddScoped<PortalApi>();
var app = builder.Build();
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllerRoute(name: "default", pattern: "{controller=Portal}/{action=Index}/{id?}");
app.Run();
