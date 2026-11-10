
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaCeDay.Data;
using RaCeDay_Part_2.Services;
using RaceDayy.Models;

namespace RaceDayy.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IBlobStorageService _blobs;

    public EventsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IBlobStorageService blobs)
    {
        _db = db;
        _users = users;
        _blobs = blobs;
    }

    // ========================================
    // INDEX - LIST ALL EVENTS
    // ========================================

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

    // ========================================
    // DETAILS - VIEW AN EVENT
    // ========================================

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound();

        var userId = _users.GetUserId(User);

        ViewBag.IsOwner = ev.OrganiserId == userId;

        ViewBag.IsEnrolled = await _db.Enrolments
            .AnyAsync(e =>
                e.EventId == ev.Id &&
                e.ParticipantId == userId);

        return View(ev);
    }

    // ========================================
    // CREATE - GET
    // ========================================

    [Authorize(Roles = "Admin,Organiser")]
    [HttpGet]
    public IActionResult Create()
    {
        var ev = new Event
        {
            Date = DateTime.Today
                .AddDays(30)
                .AddHours(8)
        };

        return View(ev);
    }

    // ========================================
    // CREATE - POST WITH BANNER UPLOAD
    // ========================================

    [Authorize(Roles = "Admin,Organiser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Date,Location,DistanceKm,EventType")]
        Event ev,
        IFormFile? bannerFile)
    {
        var userId = _users.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        // Always set the organiser on the server.
        ev.OrganiserId = userId;

        ModelState.Remove(nameof(Event.OrganiserId));

        // Validate the optional banner.
        if (bannerFile != null)
        {
            var error = _blobs.Validate(bannerFile);

            if (error != null)
            {
                ModelState.AddModelError(
                    nameof(bannerFile),
                    error);
            }
        }

        if (!ModelState.IsValid)
            return View(ev);

        BlobUploadResult? uploaded = null;

        try
        {
            // Upload image to Azure Blob Storage.
            if (bannerFile != null)
            {
                uploaded = await _blobs.UploadBannerAsync(
                    bannerFile);

                ev.BannerBlobName = uploaded.BlobName;
                ev.BannerImageURL = uploaded.ImageUrl;
            }

            // Save event information to SQL Server.
            _db.Events.Add(ev);

            await _db.SaveChangesAsync();
        }
        catch (BannerApiException ex)
        {
            ModelState.AddModelError(nameof(bannerFile), ex.Message);
            return View(ev);
        }
        catch
        {
            // Remove newly uploaded image if
            // the database operation fails.
            if (uploaded != null)
            {
                await DeleteBlobSafelyAsync(
                    uploaded.BlobName);
            }

            throw;
        }

        TempData["Success"] =
            "Event created. Now add the categories for it.";

        return RedirectToAction(
            "Index",
            "Categories",
            new { eventId = ev.Id });
    }

    // ========================================
    // EDIT - GET
    // ========================================

    [Authorize(Roles = "Admin,Organiser")]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // ========================================
    // EDIT - POST WITH BANNER REPLACEMENT
    // ========================================

    [Authorize(Roles = "Admin,Organiser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Edit(
        int id,
        IFormFile? bannerFile)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        // Validate a new image if one was selected.
        if (bannerFile != null)
        {
            var error = _blobs.Validate(bannerFile);

            if (error != null)
            {
                ModelState.AddModelError(
                    nameof(bannerFile),
                    error);
            }
        }

        // Only permit changes to editable fields.
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

        var previousBlobName = ev.BannerBlobName;

        BlobUploadResult? newUpload = null;

        try
        {
            if (bannerFile != null)
            {
                // Upload replacement banner first.
                newUpload = await _blobs.UploadBannerAsync(
                    bannerFile);

                ev.BannerBlobName = newUpload.BlobName;
                ev.BannerImageURL = newUpload.ImageUrl;
            }

            // Save updated event information.
            await _db.SaveChangesAsync();
        }
        catch (BannerApiException ex)
        {
            ModelState.AddModelError(nameof(bannerFile), ex.Message);
            return View(ev);
        }
        catch
        {
            // Delete the replacement if saving fails.
            if (newUpload != null)
            {
                await DeleteBlobSafelyAsync(
                    newUpload.BlobName);
            }

            throw;
        }

        // Only remove the old image after
        // the event has been saved successfully.
        if (newUpload != null &&
            !string.IsNullOrWhiteSpace(previousBlobName))
        {
            await DeleteBlobSafelyAsync(previousBlobName);
        }

        TempData["Success"] = "Event updated.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    // ========================================
    // DELETE - GET
    // ========================================

    [Authorize(Roles = "Admin,Organiser")]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // ========================================
    // DELETE - POST
    // ========================================

    [Authorize(Roles = "Admin,Organiser")]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        // Protect existing participant enrolments.
        var hasEnrolments = await _db.Enrolments
            .AnyAsync(e => e.EventId == id);

        if (hasEnrolments)
        {
            TempData["Error"] =
                "This event has enrolments and cannot be deleted.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var blobName = ev.BannerBlobName;

        // Delete database record first.
        _db.Events.Remove(ev);

        await _db.SaveChangesAsync();

        // Then remove the image from Azure.
        if (!string.IsNullOrWhiteSpace(blobName))
        {
            await DeleteBlobSafelyAsync(blobName);
        }

        TempData["Success"] =
            "Event deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // ========================================
    // HELPER - FIND EVENT OWNED BY USER
    // ========================================

    private Task<Event?> FindOwnedEventAsync(int id)
    {
        var userId = _users.GetUserId(User);

        return _db.Events
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.OrganiserId == userId);
    }

    // ========================================
    // HELPER - CLEAN UP AZURE IMAGE
    // ========================================

    private async Task DeleteBlobSafelyAsync(
        string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return;

        try
        {
            await _blobs.DeleteAsync(blobName);
        }
        catch (Exception ex)
        {
            // Log the failure without hiding the
            // original event database operation.
            System.Diagnostics.Trace.TraceError(
                $"Azure Blob cleanup failed: {ex}");
        }
    }
}
