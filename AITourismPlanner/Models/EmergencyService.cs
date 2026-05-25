using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class EmergencyService
    {
        [Key]
        public int service_id { get; set; }

        public int? destination_id { get; set; }

        [StringLength(255)]
        public string hospital_name { get; set; }

        [StringLength(255)]
        public string police_station { get; set; }

        [StringLength(20)]
        public string helpline_number { get; set; }

        // Navigation property
        [ForeignKey("destination_id")]
        public virtual Destination Destination { get; set; }
    }
}