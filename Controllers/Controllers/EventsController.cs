using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [Authorize(Roles = "Admin,Organiser")]
    public IActionResult Create()
    {
        return View(new Event
        {
            Date = DateTime.Today.AddDays(30).AddHours(8)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Date,Location,DistanceKm,EventType")]
        Event ev)
    {
        var userId = _users.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        ev.OrganiserId = userId;
        ModelState.Remove(nameof(Event.OrganiserId));

        if (!ModelState.IsValid)
            return View(ev);

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        TempData["Success"] =
            "Event created. Now add the categories for it.";

        return RedirectToAction(
            "Index",
            "Categories",
            new { eventId = ev.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        return ev == null ? NotFound() : View(ev);
    }

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

        bool hasBanner = bannerFile != null;

        if (hasBanner)
        {
            var error = ValidateBanner(bannerFile!);

            if (error != null)
                ModelState.AddModelError(nameof(bannerFile), error);
        }

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
            if (hasBanner)
            {
                uploaded = await UploadBannerAsync(bannerFile!);

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
            // Remove the new upload if saving the event failed.
            if (uploaded != null)
                await TryDeleteBannerAsync(uploaded.BlobName);

            ev.BannerBlobName = oldBlobName;
            ev.BannerImageURL = oldImageUrl;

            _logger.LogError(ex, "Failed to update event {EventId}.", id);

            ModelState.AddModelError(
                "",
                "The event could not be saved. Please try again.");

            return View(ev);
        }

        // Delete the old image only after the database was saved.
        if ((uploaded != null || removeBanner) &&
            !string.IsNullOrWhiteSpace(oldBlobName))
        {
            await TryDeleteBannerAsync(oldBlobName);
        }

        TempData["Success"] = "Event updated.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        return ev == null ? NotFound() : View(ev);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
        {
            TempData["Error"] =
                "This event has enrolments and cannot be deleted.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var oldBlobName = ev.BannerBlobName;

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldBlobName))
            await TryDeleteBannerAsync(oldBlobName);

        TempData["Success"] = "Event deleted.";

        return RedirectToAction(nameof(Index));
    }

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

        bool allowed = (extension, file.ContentType.ToLowerInvariant()) switch
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
            _configuration["AzureBlob:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureBlob:ConnectionString is not configured.");
        }

        return new BlobServiceClient(connectionString)
            .GetBlobContainerClient("raceday-images");
    }

    private async Task<BlobUploadResult> UploadBannerAsync(
        IFormFile file)
    {
        var container = GetBannerContainer();

        // The container must already exist in Azure.
        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var blobName = $"events/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();

        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
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
            await container.GetBlobClient(blobName)
                .DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            // Cleanup failure must not undo a saved event.
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