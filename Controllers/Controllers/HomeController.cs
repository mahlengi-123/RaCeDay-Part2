using Microsoft.AspNetCore.Mvc;
using RaceDayy.Models;
using System.Diagnostics;

namespace RaceDayy.Controllers
{
    public class HomeController : Controller
    {
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Organiser")]
        public IActionResult OrganiserDashboard()
        {
            return RedirectToAction("Index", "Dashboard");
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
