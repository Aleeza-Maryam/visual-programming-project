using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class HotelRoom
    {
        [Key]
        public int room_id { get; set; }

        public int? hotel_id { get; set; }

        [StringLength(50)]
        public string room_type { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? room_price { get; set; }

        public int? total_rooms { get; set; }

        public int? available_rooms { get; set; }

        // Navigation properties
        [ForeignKey("hotel_id")]
        public virtual Hotel Hotel { get; set; }

        public virtual ICollection<HotelBooking> HotelBookings { get; set; }
    }
}