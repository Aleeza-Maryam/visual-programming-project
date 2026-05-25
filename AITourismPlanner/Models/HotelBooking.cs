using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class HotelBooking
    {
        [Key]
        public int booking_id { get; set; }

        public int? user_id { get; set; }
        public int? hotel_id { get; set; }
        public int? room_id { get; set; }

        [DataType(DataType.Date)]
        public DateTime? check_in { get; set; }

        [DataType(DataType.Date)]
        public DateTime? check_out { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? total_amount { get; set; }

        public string booking_status { get; set; } = "Pending";

        public DateTime booking_date { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        [ForeignKey("hotel_id")]
        public virtual Hotel Hotel { get; set; }

        [ForeignKey("room_id")]
        public virtual HotelRoom HotelRoom { get; set; }
    }
}