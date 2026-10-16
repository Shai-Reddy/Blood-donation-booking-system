using System.ComponentModel.DataAnnotations;
using BloodDonation.Domain.Models;

namespace BloodDonation.Web.Models
{
    public class BookingViewModel
    {
        //session must be selected when creating a booking
        [Required]
        public string SessionId { get; set; } = string.Empty;

        //donor name is required for the booking
        [Required]
        public string DonorName { get; set; } = string.Empty;

        //donor email is required and must be a valid email address
        [Required]
        [EmailAddress]
        public string DonorEmail { get; set; } = string.Empty;

        //blood type is optional when creating a booking
        public string? BloodType { get; set; }

        //notes are optional when creating a booking
        public string? Notes { get; set; }

        //eligibility details, checked against the session date before the booking is saved (#1)
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Range(1, 300)]
        [Display(Name = "Weight (kg)")]
        public double? WeightKg { get; set; }

        //left blank by first-time donors
        [DataType(DataType.Date)]
        [Display(Name = "Last donation date (leave blank if first-time donor)")]
        public DateTime? LastDonationDate { get; set; }

        [Display(Name = "Had a recent tattoo, piercing, or illness?")]
        public bool HasStandDownEvent { get; set; }

        [Display(Name = "Reason")]
        public StandDownReason? StandDownReason { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Event date")]
        public DateTime? StandDownEventDate { get; set; }

        [Range(1, 3650)]
        [Display(Name = "Required stand-down duration (days)")]
        public int? StandDownDurationDays { get; set; }
    }
}