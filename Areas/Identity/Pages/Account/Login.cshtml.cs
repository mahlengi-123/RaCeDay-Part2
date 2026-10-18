
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RaCeDay.Data;

namespace RaCeDay.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
            = new List<AuthenticationScheme>();

        public string ReturnUrl { get; set; } = "/";

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(
                    string.Empty,
                    ErrorMessage);
            }

            ReturnUrl = Url.IsLocalUrl(returnUrl)
                ? returnUrl!
                : Url.Content("~/");

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl)
                ? returnUrl!
                : Url.Content("~/");

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find the account by email first.
            // Fallback supports the existing Admin account
            // whose username exists but Email is blank.
            var account = await _userManager
                .FindByEmailAsync(Input.Email);

            account ??= await _userManager
                .FindByNameAsync(Input.Email);

            if (account == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid login attempt.");

                return Page();
            }

            // Sign in using the actual Identity username.
            var result = await _signInManager.PasswordSignInAsync(
                account,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "User logged in successfully.");

                if (await _userManager.IsInRoleAsync(
                    account, "Admin"))
                {
                    return RedirectToAction(
                        "AdminDashboard", "Home");
                }

                if (await _userManager.IsInRoleAsync(
                    account, "Organiser"))
                {
                    return RedirectToAction(
                        "OrganiserDashboard", "Home");
                }

                if (await _userManager.IsInRoleAsync(
                    account, "Participant"))
                {
                    return LocalRedirect(ReturnUrl);
                }

                return LocalRedirect(ReturnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new
                    {
                        ReturnUrl,
                        RememberMe = Input.RememberMe
                    });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning(
                    "User account locked out.");

                return RedirectToPage("./Lockout");
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please confirm your email before logging in.");

                return Page();
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");

            return Page();
        }
    }
}
