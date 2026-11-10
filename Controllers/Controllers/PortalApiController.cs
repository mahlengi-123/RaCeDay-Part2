using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaCeDay.Data;
using RaceDayy.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDayy.Controllers;

// Part 2 owns the data and business rules. The separate Part 3 MVC project
// only communicates with this API; it has no Entity Framework or DB context.
[ApiController]
[Route("api/portal")]
public sealed class PortalApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IConfiguration _config;

    public PortalApiController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn, IConfiguration config)
    {
        _db = db; _users = users; _signIn = signIn; _config = config;
    }

    public sealed record LoginInput([Required, EmailAddress] string Email, [Required] string Password);
    public sealed record RegisterInput([Required] string FirstName, [Required] string LastName,
        [Required, EmailAddress] string Email, [Required] string Password);
    public sealed record EventInput([Required] string Name, [Required] string Description,
        DateTime Date, [Required] string Location, decimal DistanceKm, [Required] string EventType);
    public sealed record CategoryInput(int EventId, [Required] string Name, [Required] string Kind);
    public sealed record EnrolInput(int EventId, int CategoryId);
    public sealed record ResultInput(int EnrolmentId, [Required] string FinishTime, int Position);
    public sealed record ProfileInput([Required] string FirstName, [Required] string LastName, string? Gender);

    private string? UserId => _users.GetUserId(User);
    private static object MapCategory(Category c) => new { c.Id, c.EventId, c.Name, Kind = c.Kind.ToString() };
    private static object MapEvent(Event e) => new {
        e.Id, e.Name, e.Description, e.Date, e.Location, e.DistanceKm,
        EventType = e.EventType.ToString(), BannerImageUrl = e.BannerImageURL,
        e.OrganiserId, EnrolmentCount = e.Enrolments.Count,
        Categories = e.Categories.Select(MapCategory).ToArray()
    };
    private Task<Event?> OwnedEvent(int id) =>
        _db.Events.Include(x => x.Categories).Include(x => x.Enrolments)
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganiserId == UserId);

    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var user = await _users.FindByEmailAsync(input.Email);
        if (user is null) return Unauthorized(new { message = "Invalid email or password." });
        var result = await _signIn.PasswordSignInAsync(user, input.Password, false, true);
        if (!result.Succeeded)
            return Unauthorized(new { message = result.IsNotAllowed
                ? "Please confirm your email address before signing in."
                : "Invalid email or password." });
        var roles = await _users.GetRolesAsync(user);
        return Ok(new { userId = user.Id, fullName = user.FullName,
            role = roles.Contains("Admin") ? "Admin" : roles.Contains("Organiser") ? "Organiser" : "Participant" });
    }

    [AllowAnonymous]
    [HttpPost("auth/register")]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName))
            return BadRequest(new { message = "First and last names are required." });
        var user = new ApplicationUser {
            FirstName = input.FirstName.Trim(), LastName = input.LastName.Trim(),
            Email = input.Email.Trim(), UserName = input.Email.Trim(), Gender = Gender.PreferNotToSay
        };
        var result = await _users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        await _users.AddToRoleAsync(user, "Participant");
        // Do not bypass Part 2's email-confirmation requirement.
        return Ok(new { message = "Account created. Confirm your email address before signing in if confirmation is enabled." });
    }

    [Authorize]
    [HttpGet("auth/me")]
    public async Task<IActionResult> WhoAmI()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(new { userId = user.Id, name = user.FullName,
            roles = await _users.GetRolesAsync(user) });
    }

    [Authorize]
    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout() { await _signIn.SignOutAsync(); return Ok(); }

    [AllowAnonymous]
    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] string? search, [FromQuery] string? type)
    {
        var query = _db.Events.AsNoTracking().Include(e => e.Categories)
            .Include(e => e.Enrolments).Where(e => e.Date >= DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) || e.Location.Contains(search));
        var events = await query.OrderBy(e => e.Date).ToListAsync();
        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType.ToString().Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        return Ok(events.Select(MapEvent));
    }

    [AllowAnonymous]
    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> EventById(int id)
    {
        var ev = await _db.Events.AsNoTracking().Include(x => x.Categories)
            .Include(x => x.Enrolments).FirstOrDefaultAsync(x => x.Id == id);
        return ev is null ? NotFound() : Ok(MapEvent(ev));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpGet("organiser/events")]
    public async Task<IActionResult> MyEvents()
    {
        var events = await _db.Events.AsNoTracking().Include(e => e.Categories)
            .Include(e => e.Enrolments).Where(e => e.OrganiserId == UserId)
            .OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(MapEvent));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent(EventInput input)
    {
        if (!Enum.TryParse<EventType>(input.EventType, true, out var type) ||
            input.DistanceKm <= 0 || input.DistanceKm > 1000 ||
            input.Date <= DateTime.UtcNow)
            return BadRequest(new { message = "Choose a future date, a valid type and distance between 0.1 and 1000 km." });
        var ev = new Event { Name = input.Name.Trim(), Description = input.Description.Trim(),
            Date = input.Date, Location = input.Location.Trim(), DistanceKm = input.DistanceKm,
            EventType = type, OrganiserId = UserId! };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(EventById), new { id = ev.Id }, MapEvent(ev));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpPut("events/{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, EventInput input)
    {
        var ev = await OwnedEvent(id);
        if (ev is null) return NotFound();
        if (!Enum.TryParse<EventType>(input.EventType, true, out var type) ||
            input.DistanceKm <= 0 || input.DistanceKm > 1000)
            return BadRequest(new { message = "Invalid event type or distance." });
        ev.Name = input.Name.Trim(); ev.Description = input.Description.Trim();
        ev.Date = input.Date; ev.Location = input.Location.Trim();
        ev.DistanceKm = input.DistanceKm; ev.EventType = type;
        await _db.SaveChangesAsync();
        return Ok(MapEvent(ev));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await OwnedEvent(id);
        if (ev is null) return NotFound();
        if (ev.Enrolments.Any()) return Conflict(new { message = "An event with enrolments cannot be deleted." });
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpPost("categories")]
    public async Task<IActionResult> AddCategory(CategoryInput input)
    {
        if (await OwnedEvent(input.EventId) is null) return NotFound();
        if (!Enum.TryParse<CategoryKind>(input.Kind, true, out var kind) ||
            string.IsNullOrWhiteSpace(input.Name))
            return BadRequest(new { message = "Provide a category name and Age or Distance type." });
        var category = new Category { EventId = input.EventId, Name = input.Name.Trim(), Kind = kind };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(MapCategory(category));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var c = await _db.Categories.Include(x => x.Enrolments)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c is null || await OwnedEvent(c.EventId) is null) return NotFound();
        if (c.Enrolments.Any()) return Conflict(new { message = "Category has enrolments." });
        _db.Categories.Remove(c); await _db.SaveChangesAsync(); return NoContent();
    }

    [Authorize(Roles = "Participant")]
    [HttpPost("enrolments")]
    public async Task<IActionResult> Enrol(EnrolInput input)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == input.EventId);
        if (ev is null || ev.Date <= DateTime.UtcNow)
            return BadRequest(new { message = "This event is not available for enrolment." });
        if (!await _db.Categories.AnyAsync(c => c.Id == input.CategoryId && c.EventId == ev.Id))
            return BadRequest(new { message = "Select a category belonging to this event." });
        if (await _db.Enrolments.AnyAsync(e => e.EventId == ev.Id && e.ParticipantId == UserId))
            return Conflict(new { message = "You are already enrolled in this event." });
        var enrolment = new Enrolment { EventId = ev.Id, CategoryId = input.CategoryId,
            ParticipantId = UserId! };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();
        return Ok(new { enrolment.Id, status = "Confirmed" });
    }

    [Authorize(Roles = "Participant")]
    [HttpGet("me/enrolments")]
    public async Task<IActionResult> MyEnrolments()
    {
        var items = await _db.Enrolments.AsNoTracking().Where(e => e.ParticipantId == UserId)
            .Include(e => e.Event).Include(e => e.Category).Include(e => e.Result)
            .OrderByDescending(e => e.EnrolledOn).ToListAsync();
        return Ok(items.Select(e => new { e.Id, e.EventId, EventName = e.Event?.Name,
            EventDate = e.Event?.Date, Category = e.Category?.Name,
            e.EnrolledOn, status = "Confirmed", hasResult = e.Result is not null }));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpGet("organiser/events/{id:int}/enrolments")]
    public async Task<IActionResult> EventEnrolments(int id)
    {
        if (await OwnedEvent(id) is null) return NotFound();
        var items = await _db.Enrolments.AsNoTracking().Where(e => e.EventId == id)
            .Include(e => e.Participant).Include(e => e.Category).Include(e => e.Result)
            .OrderBy(e => e.Id).ToListAsync();
        return Ok(items.Select(e => new { e.Id, ParticipantName = e.Participant?.FullName,
            e.ParticipantId, Category = e.Category?.Name, e.EnrolledOn,
            status = "Confirmed", finishTime = e.Result?.FinishTime.ToString(),
            position = e.Result?.Position }));
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpPost("results")]
    public async Task<IActionResult> CaptureResult(ResultInput input)
    {
        var enrol = await _db.Enrolments.Include(e => e.Event)
            .Include(e => e.Result).FirstOrDefaultAsync(e => e.Id == input.EnrolmentId);
        if (enrol is null || enrol.Event is null || enrol.Event.OrganiserId != UserId)
            return NotFound();
        if (enrol.Event.Date > DateTime.UtcNow)
            return Conflict(new { message = "Results can only be recorded after the event." });
        if (!TimeSpan.TryParse(input.FinishTime, out var time) ||
            time <= TimeSpan.Zero || time > TimeSpan.FromDays(2) || input.Position < 1)
            return BadRequest(new { message = "Provide a valid finish time (hh:mm:ss) and position." });
        var duplicatePosition = await _db.Results.AnyAsync(r => r.EnrolmentId != enrol.Id
            && r.Enrolment!.EventId == enrol.EventId && r.Position == input.Position);
        if (duplicatePosition) return Conflict(new { message = "This position is already taken." });
        var result = enrol.Result ?? new Result { EnrolmentId = enrol.Id };
        result.FinishTime = time; result.Position = input.Position;
        if (enrol.Result is null) _db.Results.Add(result);
        await _db.SaveChangesAsync();
        return Ok(new { result.Id, result.EnrolmentId, result.FinishTime, result.Position });
    }

    [Authorize(Roles = "Participant")]
    [HttpGet("me/results")]
    public async Task<IActionResult> MyResults()
    {
        var results = await _db.Results.AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Where(r => r.Enrolment!.ParticipantId == UserId)
            .OrderByDescending(r => r.Enrolment!.Event!.Date).ToListAsync();
        var eventIds = results.Select(r => r.Enrolment!.EventId).Distinct().ToArray();
        var counts = await _db.Results.Where(r => eventIds.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId).Select(g => new { id = g.Key, count = g.Count() })
            .ToDictionaryAsync(x => x.id, x => x.count);
        return Ok(results.Select(r => new { EventName = r.Enrolment!.Event!.Name,
            EventDate = r.Enrolment.Event.Date, Category = r.Enrolment.Category?.Name,
            finishTime = r.FinishTime.ToString(), r.Position,
            totalFinishers = counts.GetValueOrDefault(r.Enrolment.EventId) }));
    }

    [Authorize]
    [HttpGet("me/profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(new { user.FirstName, user.LastName,
            user.Email, gender = user.Gender.ToString(), profileImageUrl = user.ProfileimagePath });
    }

    [Authorize]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(ProfileInput input)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (!Enum.TryParse<Gender>(input.Gender, true, out var gender))
            return BadRequest(new { message = "Choose a valid gender." });
        user.FirstName = input.FirstName.Trim(); user.LastName = input.LastName.Trim(); user.Gender = gender;
        var result = await _users.UpdateAsync(user);
        return result.Succeeded ? Ok(new { message = "Profile updated." })
            : BadRequest(new { message = "Could not update the profile." });
    }

    private async Task<string?> UploadBlob(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024 ||
            !new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
            return null;
        var connection = _config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection)) return null;
        var container = new BlobServiceClient(connection).GetBlobContainerClient("raceday-images");
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var ext = file.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        var blob = container.GetBlobClient(folder + "/" + Guid.NewGuid().ToString("N") + ext);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }

    [Authorize(Roles = "Organiser,Admin")]
    [HttpPost("events/{id:int}/banner")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await OwnedEvent(id);
        if (ev is null) return NotFound();
        try
        {
            var url = await UploadBlob(file, "events");
            if (url is null)
                return BadRequest(new { message = "Upload a JPG, PNG or WebP under 5 MB; Azure Blob Storage must be configured." });
            ev.BannerImageURL = url;
            await _db.SaveChangesAsync();
            return Ok(new { imageUrl = url });
        }
        catch (Exception) { return StatusCode(503, new { message = "Image storage is unavailable. Please try again." }); }
    }

    [Authorize]
    [HttpPost("me/profile/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        try
        {
            var url = await UploadBlob(file, "profiles");
            if (url is null)
                return BadRequest(new { message = "Upload a JPG, PNG or WebP under 5 MB; Azure Blob Storage must be configured." });
            user.ProfileimagePath = url;
            await _users.UpdateAsync(user);
            return Ok(new { imageUrl = url });
        }
        catch (Exception) { return StatusCode(503, new { message = "Image storage is unavailable. Please try again." }); }
    }
}