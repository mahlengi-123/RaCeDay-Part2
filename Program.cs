
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using RaCeDay.Data;
using RaCeDay.Models.Interface;

var builder = WebApplication.CreateBuilder(args);

// Development email service
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender, DevelopmentEmailSender>();
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

// Database, roles and Admin setup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services
        .GetRequiredService<ApplicationDbContext>();

    // Apply pending migrations
    context.Database.Migrate();

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

    var userManager = services
        .GetRequiredService<UserManager<ApplicationUser>>();

    string? email = builder.Configuration["SeedAdmin:Email"];
    string? password = builder.Configuration["SeedAdmin:Password"];

    // Create Admin only if the account does not exist
    if (!string.IsNullOrWhiteSpace(email) &&
        !string.IsNullOrWhiteSpace(password))
    {
        var admin = await userManager.FindByEmailAsync(email);

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

            var addRoleResult = await userManager.AddToRoleAsync(
                admin,
                "Admin");

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin role assignment failed: " +
                    string.Join("; ",
                        addRoleResult.Errors.Select(
                            e => e.Description)));
            }
        }
    }

    // TEMPORARY: Confirm trusted local test accounts only.
    // Remove this block after confirming both accounts.
    // This bypasses actual email ownership verification.
    if (app.Environment.IsDevelopment())
    {
        var testEmails = new[]
        {
            "amahlesithole2003@gmail.com",
            "amahleyeyeza2003@gmail.com"
        };

        foreach (var testEmail in testEmails)
        {
            var participant = await userManager
                .FindByEmailAsync(testEmail);

            if (participant == null ||
                participant.EmailConfirmed ||
                !await userManager.IsInRoleAsync(
                    participant, "Participant"))
            {
                continue;
            }

            var token = await userManager
                .GenerateEmailConfirmationTokenAsync(participant);

            var confirmResult = await userManager
                .ConfirmEmailAsync(participant, token);

            if (!confirmResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Participant confirmation failed: " +
                    string.Join("; ",
                        confirmResult.Errors.Select(
                            e => e.Description)));
            }
        }
    }
    // END TEMPORARY PARTICIPANT CONFIRMATION
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();















