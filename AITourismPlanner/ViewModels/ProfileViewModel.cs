using System.ComponentModel.DataAnnotations;

namespace AITourismPlanner.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Profile Image")]
        public string ProfileImage { get; set; }

        [Display(Name = "Preferred Budget (PKR)")]
        public decimal? PreferredBudget { get; set; }

        [Display(Name = "Favorite Category")]
        public string FavoriteCategory { get; set; }

        [Display(Name = "Preferred Transport")]
        public string PreferredTransport { get; set; }

        [Display(Name = "Preferred Season")]
        public string PreferredSeason { get; set; }
    }
}