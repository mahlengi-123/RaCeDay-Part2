using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaCeDay.Data;
using RaCeDay.Models;
using RaceDayy.Models;

namespace RaCeDay.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CategoriesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }
    
    // Display categories for an event
    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
            return NotFound();

        ViewBag.Event = ev;
        ViewBag.IsOwner =.
            User.IsInRole("Admin") ||
            ev.OrganiserId == _users.GetUserId(User);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    // Display the create form
    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create(int eventId)
    {
        var ev = await FindOwnedEventAsync(eventId);

        if (ev == null)
            return NotFound();

        ViewBag.EventName = ev.Name;

        return View(new Category
        {
            EventId = eventId
        });
    }

    // Save a new category
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create(
        [Bind("EventId,Name,Kind")] Category category)
    {
        var ev = await FindOwnedEventAsync(category.EventId);

        if (ev == null)
            return NotFound();

        // The event is checked above and is not entered in the form.
        ModelState.Remove(nameof(Category.Event));

        var duplicateExists = await _db.Categories.AnyAsync(c =>
            c.EventId == category.EventId &&
            c.Name == category.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(Category.Name),
                "This event already has a category with that name.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.EventName = ev.Name;
            return View(category);
        }

        category.Event = ev;

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Category added.";

        return RedirectToAction(
            nameof(Index),
            new { eventId = category.EventId });
    }

    // Display the edit form
    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
            return NotFound();

        var category = await FindOwnedCategoryAsync(id.Value);

        if (category == null)
            return NotFound();

        ViewBag.EventName = category.Event!.Name;

        return View(category);
    }

    // Save changes to a category
    [HttpPost]
    [ActionName("Edit")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> EditConfirmed(int id)
    {
        var category = await FindOwnedCategoryAsync(id);

        if (category == null)
            return NotFound();

        // Only allow these two fields to be changed.
        var updated = await TryUpdateModelAsync(
            category,
            "",
            c => c.Name,
            c => c.Kind);

        if (updated)
        {
            var duplicateExists = await _db.Categories.AnyAsync(c =>
                c.EventId == category.EventId &&
                c.Name == category.Name &&
                c.Id != id);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(Category.Name),
                    "This event already has a category with that name.");
            }
        }

        if (!updated || !ModelState.IsValid)
        {
            ViewBag.EventName = category.Event!.Name;
            return View("Edit", category);
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Category updated.";

        return RedirectToAction(
            nameof(Index),
            new { eventId = category.EventId });
    }

    // Display the delete confirmation
    [HttpGet]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
            return NotFound();

        var category = await FindOwnedCategoryAsync(id.Value);

        if (category == null)
            return NotFound();

        ViewBag.EventName = category.Event!.Name;

        return View(category);
    }

    // Delete a category if nobody is enrolled
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await FindOwnedCategoryAsync(id);

        if (category == null)
            return NotFound();

        var hasEnrolments = await _db.Enrolments
            .AnyAsync(e => e.CategoryId == id);

        if (hasEnrolments)
        {
            TempData["Error"] =
                "Participants are enrolled in this category, so it cannot be deleted.";

            return RedirectToAction(
                nameof(Index),
                new { eventId = category.EventId });
        }

        var eventId = category.EventId;

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Category deleted.";

        return RedirectToAction(nameof(Index), new { eventId });
    }

    // Admins can access any event; Organisers only their own.
    private Task<Event?> FindOwnedEventAsync(int eventId)
    {
        var userId = _users.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        return _db.Events.FirstOrDefaultAsync(e =>
            e.Id == eventId &&
            (isAdmin || e.OrganiserId == userId));
    }

    // Apply the same ownership check to categories.
    private Task<Category?> FindOwnedCategoryAsync(int id)
    {
        var userId = _users.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        return _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.Event != null &&
                (isAdmin || c.Event.OrganiserId == userId));
    }
}