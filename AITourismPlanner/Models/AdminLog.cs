using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class AdminLog
    {
        [Key]
        public int log_id { get; set; }

        public int? admin_id { get; set; }

        public string action { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("admin_id")]
        public virtual User Admin { get; set; }
    }
}