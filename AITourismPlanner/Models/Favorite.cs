using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Favorite
    {
        [Key]
        public int favorite_id { get; set; }

        public int? user_id { get; set; }
        public int? destination_id { get; set; }

        // Navigation properties
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        [ForeignKey("destination_id")]
        public virtual Destination Destination { get; set; }
    }
}