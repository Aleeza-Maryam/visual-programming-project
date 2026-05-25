using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Trip
    {
        [Key]
        public int trip_id { get; set; }

        public int? user_id { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? total_budget { get; set; }

        public int? total_days { get; set; }

        [StringLength(100)]
        public string trip_type { get; set; }

        [DataType(DataType.Date)]
        public DateTime? start_date { get; set; }

        [DataType(DataType.Date)]
        public DateTime? end_date { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        public virtual ICollection<Itinerary> Itineraries { get; set; }
    }
}