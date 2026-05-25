using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class AIRecommendation
    {
        [Key]
        public int recommendation_id { get; set; }

        public int? user_id { get; set; }

        [StringLength(255)]
        public string recommended_destination { get; set; }

        public string recommendation_reason { get; set; }

        public DateTime generated_at { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("user_id")]
        public virtual User User { get; set; }
    }
}