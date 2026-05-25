using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Hotel
    {
        [Key]
        public int hotel_id { get; set; }

        public int? destination_id { get; set; }

        [Required]
        [StringLength(100)]
        public string hotel_name { get; set; }

        public string address { get; set; }

        [Column(TypeName = "decimal(2,1)")]
        public decimal? star_rating { get; set; }

        public string description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? price_per_night { get; set; }

        [StringLength(20)]
        public string contact_number { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string email { get; set; }

        [StringLength(255)]
        public string image { get; set; }

        // Navigation properties
        [ForeignKey("destination_id")]
        public virtual Destination Destination { get; set; }

        public virtual ICollection<HotelRoom> HotelRooms { get; set; }
        public virtual ICollection<HotelBooking> HotelBookings { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}