using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Destination
    {
        [Key]
        public int destination_id { get; set; }

        [Required]
        [StringLength(100)]
        public string name { get; set; }

        [StringLength(100)]
        public string city { get; set; }

        [StringLength(100)]
        public string country { get; set; }

        public string description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? estimated_cost { get; set; }

        [StringLength(50)]
        public string best_season { get; set; }

        [Column(TypeName = "decimal(10,6)")]
        public decimal? latitude { get; set; }

        [Column(TypeName = "decimal(10,6)")]
        public decimal? longitude { get; set; }

        public int? category_id { get; set; }

        [StringLength(255)]
        public string thumbnail { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal rating_average { get; set; } = 0;

        public DateTime created_at { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("category_id")]
        public virtual Category Category { get; set; }

        public virtual ICollection<DestinationImage> DestinationImages { get; set; }
        public virtual ICollection<Hotel> Hotels { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<Weather> Weathers { get; set; }
        public virtual ICollection<EmergencyService> EmergencyServices { get; set; }
    }
}