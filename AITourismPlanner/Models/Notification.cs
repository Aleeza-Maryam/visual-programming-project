using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Notification
    {
        [Key]
        public int notification_id { get; set; }

        public int? user_id { get; set; }

        [StringLength(255)]
        public string title { get; set; }

        public string message { get; set; }

        public bool is_read { get; set; } = false;

        public DateTime created_at { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("user_id")]
        public virtual User User { get; set; }
    }
}