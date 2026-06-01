using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Transport
    {
        [Key]
        public int transport_id { get; set; }

        [Required]
        [StringLength(100)]
        public string departure_city { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string arrival_city { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string transport_type { get; set; } = string.Empty; // Bus, Train, Flight

        [StringLength(100)]
        public string company_name { get; set; } = string.Empty;

        [StringLength(50)]
        public string departure_time { get; set; } = string.Empty;

        [StringLength(50)]
        public string duration { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? fare { get; set; }

        public int? available_seats { get; set; } = 50;

        public DateTime created_at { get; set; } = DateTime.Now;

    }
}