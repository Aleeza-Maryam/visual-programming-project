using System.ComponentModel.DataAnnotations;

namespace AITourismPlanner.Models
{
    public class Role
    {
        [Key]
        public int role_id { get; set; }

        [Required]
        [StringLength(50)]
        public string role_name { get; set; }

        // Navigation property
        public virtual ICollection<User> Users { get; set; }
    }
}