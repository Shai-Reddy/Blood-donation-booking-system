using BloodDonation.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using BloodDonation.Domain;
using BloodDonation.Domain.Models;
using BloodDonation.Web.Areas.Coordinator.Models;
using BloodDonation.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Web.Areas.Coordinator.Controllers;

// Only coordinators can access this controller
[Area("Coordinator")]
[Authorize(Roles = Roles.Coordinator)]
public class HomeController : Controller
{
    private readonly AppointmentAnalyticsService _analyticsService;
    private readonly SampleDataStore _store;

    // Getting the analytics service and the appointment data we need for the dashboard
    public HomeController(AppointmentAnalyticsService analyticsService, SampleDataStore store)
    {
        _analyticsService = analyticsService;
        _store = store;
    }

    public IActionResult Index()
    {
        // Calculate the appointment analytics using the appointments we have stored
        var analytics = _analyticsService.Calculate(_store.Appointments);

        // Go through each appointment status and count how many appointments have that status
        var countsByStatus = Enum.GetValues<AppointmentStatus>()
            .Select(status => (status, _store.Appointments.Count(a => a.Status == status)))
            .ToList();

        // Put all the calculated information together so we can display it on the coordinator dashboard
        var model = new DashboardViewModel
        {
            DeferralRatePercentage = analytics.DeferralRatePercentage,
            AverageBookingLeadTimeDays = analytics.AverageBookingLeadTimeDays,
            AppointmentCountsByStatus = countsByStatus,
            TotalAppointments = _store.Appointments.Count
        };

        // Send the dashboard data to the view
        return View(model);
    }
}