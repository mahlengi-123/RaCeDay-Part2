using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RaCeDay.Data;
using RaceDayy.Models;

namespace RaceDayy.Controllers;

[Authorize]
public class EventsController : Controller
{
    private const long MaxBannerBytes = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IConfiguration configuration,
        ILogger<EventsController> logger)
    {
        _db = db;
        _users = users;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: Events
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.UserId = _users.GetUserId(User);

        var events = await _db.Events
            .Include(e => e.Organiser)
            .AsNoTracking()
            .OrderBy(e => e.Date)
            .ToListAsync();

        return View(events);
    }

    // GET: Events/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value);

        if (ev == null)
            return NotFound();

        var userId = _users.GetUserId(User);

        ViewBag.IsOwner = ev.OrganiserId == userId;

        ViewBag.IsEnrolled = await _db.Enrolments
            .AnyAsync(en =>
                en.EventId == ev.Id &&
                en.ParticipantId == userId);

        return View(ev);
    }

    // GET: Events/Create
    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public IActionResult Create()
    {
        return View(new Event
        {
            Date = DateTime.Today.AddDays(30).AddHours(8)
        });
    }

    // POST: Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Date,Location,DistanceKm,EventType")]
        Event ev,
        IFormFile? bannerFile)
    {
        var userId = _users.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        ev.OrganiserId = userId;

        // This value is supplied by the server, not the form.
        ModelState.Remove(nameof(Event.OrganiserId));

        if (bannerFile != null)
        {
            var error = ValidateBanner(bannerFile);

            if (error != null)
                ModelState.AddModelError(nameof(bannerFile), error);
        }

        if (!ModelState.IsValid)
            return View(ev);

        BlobUploadResult? uploaded = null;

        try
        {
            if (bannerFile != null)
            {
                uploaded = await UploadBannerAsync(bannerFile);

                ev.BannerBlobName = uploaded.BlobName;
                ev.BannerImageURL = uploaded.Url;
            }

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            if (uploaded != null)
                await TryDeleteBannerAsync(uploaded.BlobName);

            _db.Entry(ev).State = EntityState.Detached;

            ev.BannerBlobName = null;
            ev.BannerImageURL = null;

            _logger.LogError(ex, "Failed to create event.");

            ModelState.AddModelError(
                "",
                "The event could not be saved. Please check the storage " +
                "configuration and select your image again.");

            return View(ev);
        }

        TempData["Success"] =
            "Event created. Now add the categories for it.";

        return RedirectToAction(
            "Index",
            "Categories",
            new { eventId = ev.Id });
    }

    // GET: Events/Edit/5
    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // POST: Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(
        int id,
        IFormFile? bannerFile,
        bool removeBanner = false)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        if (bannerFile != null)
        {
            var error = ValidateBanner(bannerFile);

            if (error != null)
                ModelState.AddModelError(nameof(bannerFile), error);
        }

        // Only these fields can be changed through the form.
        var updated = await TryUpdateModelAsync(
            ev,
            "",
            e => e.Name,
            e => e.Description,
            e => e.Date,
            e => e.Location,
            e => e.DistanceKm,
            e => e.EventType);

        if (!updated || !ModelState.IsValid)
            return View(ev);

        var oldBlobName = ev.BannerBlobName;
        var oldImageUrl = ev.BannerImageURL;

        BlobUploadResult? uploaded = null;

        try
        {
            // A new upload takes priority over the remove checkbox.
            if (bannerFile != null)
            {
                uploaded = await UploadBannerAsync(bannerFile);

                ev.BannerBlobName = uploaded.BlobName;
                ev.BannerImageURL = uploaded.Url;
            }
            else if (removeBanner)
            {
                ev.BannerBlobName = null;
                ev.BannerImageURL = null;
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            if (uploaded != null)
                await TryDeleteBannerAsync(uploaded.BlobName);

            ev.BannerBlobName = oldBlobName;
            ev.BannerImageURL = oldImageUrl;

            _logger.LogError(
                ex,
                "Failed to update event {EventId}.",
                id);

            ModelState.AddModelError(
                "",
                "The event could not be saved. Please try again. " +
                "If you selected an image, select it again.");

            return View(ev);
        }

        // Remove the old blob only after the event was saved.
        if ((uploaded != null || removeBanner) &&
            !string.IsNullOrWhiteSpace(oldBlobName))
        {
            await TryDeleteBannerAsync(oldBlobName);
        }

        TempData["Success"] = "Event updated.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Events/Delete/5
    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // POST: Events/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        if (await _db.Enrolments.AnyAsync(en => en.EventId == id))
        {
            TempData["Error"] =
                "This event has enrolments and cannot be deleted.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var oldBlobName = ev.BannerBlobName;

        try
        {
            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete event {EventId}.",
                id);

            TempData["Error"] =
                "The event could not be deleted. Please try again.";

            return RedirectToAction(nameof(Details), new { id });
        }

        if (!string.IsNullOrWhiteSpace(oldBlobName))
            await TryDeleteBannerAsync(oldBlobName);

        TempData["Success"] = "Event deleted.";

        return RedirectToAction(nameof(Index));
    }

    // Restrict changes to events belonging to the signed-in user.
    private Task<Event?> FindOwnedEventAsync(int id)
    {
        var userId = _users.GetUserId(User);

        return _db.Events.FirstOrDefaultAsync(e =>
            e.Id == id &&
            e.OrganiserId == userId);
    }

    private static string? ValidateBanner(IFormFile file)
    {
        if (file.Length == 0)
            return "Please select a non-empty image.";

        if (file.Length > MaxBannerBytes)
            return "The banner image must be 5 MB or smaller.";

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var contentType = (file.ContentType ?? "")
            .ToLowerInvariant();

        bool allowed = (extension, contentType) switch
        {
            (".jpg", "image/jpeg") => true,
            (".jpeg", "image/jpeg") => true,
            (".png", "image/png") => true,
            (".webp", "image/webp") => true,
            _ => false
        };

        return allowed
            ? null
            : "Please upload a JPG, PNG or WebP image.";
    }

    private BlobContainerClient GetBannerContainer()
    {
        var connectionString =
            _configuration["AzureStorage:ConnectionString"];

        var containerName =
            _configuration["AzureStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureStorage:ConnectionString is not configured.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "AzureStorage:ContainerName is not configured.");
        }

        return new BlobServiceClient(connectionString)
            .GetBlobContainerClient(containerName);
    }

    private async Task<BlobUploadResult> UploadBannerAsync(
        IFormFile file)
    {
        // Use the existing Azure container.
        var container = GetBannerContainer();

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => throw new InvalidOperationException(
                "Unsupported banner image type.")
        };

        var blobName = $"events/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();

        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            });

        return new BlobUploadResult(
            blobName,
            blob.Uri.AbsoluteUri);
    }

    private async Task TryDeleteBannerAsync(string blobName)
    {
        try
        {
            var container = GetBannerContainer();

            await container
                .GetBlobClient(blobName)
                .DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            // A cleanup failure must not undo a saved event.
            _logger.LogWarning(
                ex,
                "Could not delete banner blob {BlobName}.",
                blobName);
        }
    }

    private sealed record BlobUploadResult(
        string BlobName,
        string Url);
}