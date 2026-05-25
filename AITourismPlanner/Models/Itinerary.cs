using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Itinerary
    {
        [Key]
        public int itinerary_id { get; set; }

        public int? trip_id { get; set; }
        public int? day_number { get; set; }
        public string activity { get; set; }

        [StringLength(255)]
        public string location { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? estimated_cost { get; set; }

        [StringLength(20)]
        public string activity_time { get; set; }

        [StringLength(50)]
        public string duration { get; set; }

        // Navigation property
        [ForeignKey("trip_id")]
        public virtual Trip Trip { get; set; }
    }
}