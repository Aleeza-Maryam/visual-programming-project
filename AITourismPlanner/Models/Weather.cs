using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Weather
    {
        [Key]
        public int weather_id { get; set; }

        public int? destination_id { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? temperature { get; set; }

        [StringLength(100)]
        public string weather_condition { get; set; }

        public int? humidity { get; set; }

        public DateTime updated_at { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("destination_id")]
        public virtual Destination Destination { get; set; }
    }
}