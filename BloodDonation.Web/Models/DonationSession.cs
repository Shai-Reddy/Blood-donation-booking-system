namespace BloodDonation.Web.Models
{
    // Holds the details for a donation session
    public class DonationSession
    {
        // The ID used to identify the session
        public string Id { get; set; } = string.Empty;
        // The date the session takes place
        public DateTime SessionDate { get; set; }
        // The time of the session
        public string Time { get; set; } = string.Empty;
        // Where the session is being held
        public string Location { get; set; } = string.Empty;
        // The total number of bookings the session can take
        public int Capacity { get; set; }
        // How many bookings the session currently has
        public int BookedCount { get; set; }
        // Works out how many spaces are left
        public int SpotsLeft => Capacity - BookedCount;
    }
}