using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Booking
    {
        [Key]
        public int booking_id { get; set; }

        public int user_id { get; set; }

        [StringLength(255)]
        public string destination_name { get; set; } = string.Empty;

        [StringLength(255)]
        public string hotel_name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? hotel_price_per_night { get; set; }

        public int? transport_id { get; set; }

        public DateTime check_in_date { get; set; }

        public DateTime check_out_date { get; set; }

        public int number_of_guests { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? total_price { get; set; }

        [StringLength(20)]
        public string booking_status { get; set; } = "Pending";

        [StringLength(20)]
        public string payment_status { get; set; } = "Pending";

        [StringLength(50)]
        public string booking_reference { get; set; } = string.Empty;

        public DateTime created_at { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }

        [ForeignKey("transport_id")]
        public virtual Transport? Transport { get; set; }
    }
}