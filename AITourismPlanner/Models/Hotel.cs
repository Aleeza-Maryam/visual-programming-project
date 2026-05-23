namespace AITourismPlanner.Models
{
    public class Hotel
    {
        public int HotelId { get; set; }

        public string HotelName { get; set; }

        public string Address { get; set; }

        public decimal PricePerNight { get; set; }

        public decimal StarRating { get; set; }
    }
}