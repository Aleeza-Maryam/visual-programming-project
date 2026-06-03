using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Wishlist
    {
        [Key]
        public int wishlist_id { get; set; }

        public int user_id { get; set; }

        public int destination_id { get; set; }

        [StringLength(255)]
        public string destination_name { get; set; } = string.Empty;

        [StringLength(255)]
        public string destination_city { get; set; } = string.Empty;

        [StringLength(100)]
        public string destination_country { get; set; } = "Pakistan";

        [Column(TypeName = "decimal(10,2)")]
        public decimal? estimated_cost { get; set; }

        [StringLength(500)]
        public string image_url { get; set; } = string.Empty;

        public DateTime added_date { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }
    }
}