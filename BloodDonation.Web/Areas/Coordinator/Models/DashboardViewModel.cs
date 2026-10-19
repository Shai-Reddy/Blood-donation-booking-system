using BloodDonation.Domain.Models;

namespace BloodDonation.Web.Areas.Coordinator.Models;

// Holds the information needed for the coordinator dashboard
public class DashboardViewModel
{
    // Shows the percentage of appointments where the donor was deferred
    public double DeferralRatePercentage { get; set; }
    // Shows how many days in advance appointments are booked on average
    public double AverageBookingLeadTimeDays { get; set; }
    // Holds each appointment status and how many appointments have that status
    public IReadOnlyList<(AppointmentStatus Status, int Count)> AppointmentCountsByStatus { get; set; } = Array.Empty<(AppointmentStatus, int)>();
    // Total number of appointments in the system
    public int TotalAppointments { get; set; }
}