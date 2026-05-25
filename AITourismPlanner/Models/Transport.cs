using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Transport
    {
        [Key]
        public int transport_id { get; set; }

        [StringLength(50)]
        public string transport_type { get; set; }

        [StringLength(100)]
        public string company_name { get; set; }

        [StringLength(100)]
        public string departure_city { get; set; }

        [StringLength(100)]
        public string arrival_city { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? fare { get; set; }

        public int? available_seats { get; set; }

        // Navigation property
        public virtual ICollection<TransportBooking> TransportBookings { get; set; }
    }
}