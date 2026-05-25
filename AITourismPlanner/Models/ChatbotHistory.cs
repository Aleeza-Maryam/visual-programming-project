using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AITourismPlanner.Models
{
    public class ChatbotHistory
    {
        [Key]
        public int chat_id { get; set; }

        public int? user_id { get; set; }

        public string user_message { get; set; }

        public string bot_response { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("user_id")]
        public virtual User User { get; set; }
    }
}