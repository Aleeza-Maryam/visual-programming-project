using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class DestinationReview
    {
        [Key]
        public int review_id { get; set; }

        public int user_id { get; set; }

        [StringLength(255)]
        public string destination_name { get; set; } = string.Empty;

        public int? rating { get; set; }

        [StringLength(1000)]
        public string comment { get; set; } = string.Empty;

        public DateTime created_at { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }
    }
}