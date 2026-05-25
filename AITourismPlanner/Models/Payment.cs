using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Payment
    {
        [Key]
        public int payment_id { get; set; }

        public int? user_id { get; set; }

        [StringLength(100)]
        public string booking_reference { get; set; }

        [StringLength(50)]
        public string payment_method { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? amount { get; set; }

        public string payment_status { get; set; } = "Pending";

        public DateTime payment_date { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("user_id")]
        public virtual User User { get; set; }
    }
}