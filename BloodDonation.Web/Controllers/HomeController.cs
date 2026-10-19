using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Web.Models;

namespace BloodDonation.Web.Controllers;

// Handles the homepage, privacy page and error page
public class HomeController : Controller
{
    // Opens the homepage
    public IActionResult Index()
    {
        return View();
    }

    // Opens the privacy page
    public IActionResult Privacy()
    {
        return View();
    }

    // Stops the error response from being cached
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Passes the request ID to the error page so we can trace the request
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}