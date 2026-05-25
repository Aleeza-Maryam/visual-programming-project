using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class DestinationImage
    {
        [Key]
        public int image_id { get; set; }

        public int? destination_id { get; set; }

        [StringLength(255)]
        public string image_url { get; set; }

        // Navigation property
        [ForeignKey("destination_id")]
        public virtual Destination Destination { get; set; }
    }
}