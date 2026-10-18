
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using RaCeDay.Data;
using RaCeDay.Models.Interface;

var builder = WebApplication.CreateBuilder(args);

// Development email service
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<
        IEmailSender,
        DevelopmentEmailSender>();
}

// Database connection
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ASP.NET Core Identity
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Profile image service
builder.Services.AddScoped<
    IProfilImageService,
    ProfilImageService>();

var app = builder.Build();

// Database migrations, roles and Admin setup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services
        .GetRequiredService<ApplicationDbContext>();

    // Apply pending migrations
    await context.Database.MigrateAsync();

    // Create required roles
    var roleManager = services
        .GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[]
    {
        "Admin",
        "Organiser",
        "Participant"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Role creation failed: " +
                    string.Join("; ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }
        }
    }

    // Get ApplicationUser manager
    var userManager = services
        .GetRequiredService<UserManager<ApplicationUser>>();

    // Admin credentials from configuration
    string? email = builder.Configuration["SeedAdmin:Email"];
    string? password = builder.Configuration["SeedAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(email) &&
        !string.IsNullOrWhiteSpace(password))
    {
        // Look for Admin by email OR username
        var admin = await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByNameAsync(email);


        if (admin != null &&
            app.Environment.IsDevelopment() &&
            await userManager.IsInRoleAsync(admin, "Admin") &&
            string.IsNullOrWhiteSpace(admin.Email))
        {
            var result = await userManager.SetEmailAsync(
                admin,
                email);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin email update failed: " +
                    string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            }

            // Existing trusted local Admin account only.
            var token = await userManager
                .GenerateEmailConfirmationTokenAsync(admin);

            var confirmation = await userManager
                .ConfirmEmailAsync(admin, token);

            if (!confirmation.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin email confirmation failed: " +
                    string.Join("; ",
                        confirmation.Errors.Select(e => e.Description)));
            }
        }


        // Create Admin only if it does not already exist
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                admin,
                password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin creation failed: " +
                    string.Join("; ",
                        createResult.Errors.Select(
                            e => e.Description)));
            }

            // Assign Admin role to newly created account
            var roleResult = await userManager.AddToRoleAsync(
                admin,
                "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin role assignment failed: " +
                    string.Join("; ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }
        }
        else
        {
            // Existing user found.
            // Do not overwrite their email, password or roles.
            // This prevents duplicate account creation.
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must run before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Identity Razor Pages
app.MapRazorPages();

app.Run();
