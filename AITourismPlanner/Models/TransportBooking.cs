using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class TransportBooking
    {
        [Key]
        public int booking_id { get; set; }

        public int? user_id { get; set; }
        public int? transport_id { get; set; }
        public int? seats_booked { get; set; }

        [DataType(DataType.Date)]
        public DateTime? journey_date { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? total_price { get; set; }

        // Navigation properties
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        [ForeignKey("transport_id")]
        public virtual Transport Transport { get; set; }
    }
}