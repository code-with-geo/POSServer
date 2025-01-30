using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class StockAdjustments
    {
        public int AdjustmentId { get; set; }

        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Products? Products { get; set; }
        public int Units { get; set; }
        public string? Reason { get; set; }
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public Users? Users { get; set; }
        public int? LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Locations? Locations { get; set; }
        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (add) or 1 (remove).")]
        public int Actions { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Default value
    }
}
