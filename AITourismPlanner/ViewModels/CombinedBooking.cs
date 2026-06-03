namespace AITourismPlanner.ViewModels
{
    public class CombinedBooking
    {
        public int BookingId { get; set; }
        public string BookingType { get; set; } = string.Empty; // "Hotel & Transport" or "Package"
        public string PackageName { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string TransportName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime TravelDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string BookingReference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}