
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaCeDay.Data;
using RaCeDay.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaCeDay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRoles : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRoles(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /UserRoles
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .ToListAsync();

            var model = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                model.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return View(model);
        }

        // GET: /UserRoles/Manage?userId=xxx
        [HttpGet]
        public async Task<IActionResult> Manage(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.userId = userId;

            var userRoles = await _userManager.GetRolesAsync(user);

            var roles = await _roleManager.Roles
                .AsNoTracking()
                .ToListAsync();

            var model = new List<ManageUserRolesViewModel>();

            foreach (var role in roles)
            {
                model.Add(new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    isSelected = role.Name != null &&
                        userRoles.Contains(
                            role.Name,
                            StringComparer.OrdinalIgnoreCase)
                });
            }

            return View(model);
        }

        // POST: /UserRoles/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(
            List<ManageUserRolesViewModel> model,
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.userId = userId;

            if (model == null)
            {
                return BadRequest();
            }

            var availableRoles = await _roleManager.Roles
                .AsNoTracking()
                .ToListAsync();

            var requestedIds = model
                .Where(x => x.isSelected)
                .Select(x => x.RoleId)
                .ToHashSet();

            var selectedRoles = availableRoles
                .Where(r => requestedIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToList();

            // Prevent removing your own Admin access
            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId &&
                !selectedRoles.Contains(
                    "Admin",
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You cannot remove your own Admin role.");

                return await Manage(userId);
            }

            var existingRoles = await _userManager.GetRolesAsync(user);

            var rolesToRemove = existingRoles
                .Except(selectedRoles, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rolesToAdd = selectedRoles
                .Except(existingRoles, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager
                    .RemoveFromRolesAsync(user, rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager
                    .AddToRolesAsync(user, rolesToAdd);

                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
