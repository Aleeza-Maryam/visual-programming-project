using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Review
    {
        [Key]
        public int review_id { get; set; }

        public int? user_id { get; set; }
        public int? destination_id { get; set; }
        public int? hotel_id { get; set; }

        [Range(1, 5)]
        public int? rating { get; set; }

        public string review_text { get; set; }

        public DateTime review_date { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        [ForeignKey("destination_id")]
        public virtual Destination Destination { get; set; }

        [ForeignKey("hotel_id")]
        public virtual Hotel Hotel { get; set; }
    }
}