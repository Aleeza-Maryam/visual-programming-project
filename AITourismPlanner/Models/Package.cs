using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class Package
    {
        [Key]
        public int package_id { get; set; }

        [Required]
        [StringLength(255)]
        public string package_name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string destination_name { get; set; } = string.Empty;

        // Optional fields ko nullable '?' kiya taake data na phanse
        [StringLength(50)]
        public string? package_type { get; set; }

        public int duration_nights { get; set; }
        public int duration_days { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal price_per_person { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal group_discount_percent { get; set; }

        [StringLength(255)]
        public string? hotel_name { get; set; }

        public int? hotel_stars { get; set; }

        [StringLength(100)]
        public string? hotel_room_type { get; set; }

        [StringLength(50)]
        public string? transport_type { get; set; }

        [StringLength(100)]
        public string? transport_company { get; set; }

        [StringLength(255)]
        public string? meals_included { get; set; }

        public string? inclusions { get; set; }
        public string? exclusions { get; set; }
        public string? daily_itinerary { get; set; }
        public string? special_features { get; set; }

        [StringLength(500)]
        public string? cover_image { get; set; }

        public string? gallery_images { get; set; }

        public int max_group_size { get; set; } = 20;
        public int min_booking_days { get; set; } = 1;
        public string? cancellation_policy { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_at { get; set; } = DateTime.Now;

        // Navigation properties (Made nullable to avoid EF loading crash)
        public virtual ICollection<PackageBooking>? PackageBookings { get; set; }
        public virtual ICollection<PackageReview>? PackageReviews { get; set; }
    }

    public class PackageBooking
    {
        [Key]
        public int booking_id { get; set; }
        public int user_id { get; set; }
        public int package_id { get; set; }

        [StringLength(50)]
        public string booking_reference { get; set; } = string.Empty;

        public DateTime travel_date { get; set; }
        public int number_of_adults { get; set; } = 1;
        public int number_of_children { get; set; } = 0;

        // CRITICAL FIX: Optional user inputs made Nullable with '?'
        public string? special_requests { get; set; }
        public string? dietary_needs { get; set; }
        public string? room_preferences { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal total_price { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal discount_applied { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal final_price { get; set; }

        [StringLength(20)]
        public string booking_status { get; set; } = "Pending";

        [StringLength(20)]
        public string payment_status { get; set; } = "Pending";

        public DateTime booking_date { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }

        [ForeignKey("package_id")]
        public virtual Package? Package { get; set; }
    }

    public class PackageReview
    {
        [Key]
        public int review_id { get; set; }
        public int user_id { get; set; }
        public int package_id { get; set; }
        public int? rating { get; set; }
        public string? comment { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }

        [ForeignKey("package_id")]
        public virtual Package? Package { get; set; }
    }
}