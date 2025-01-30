using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class StockIn
    {
        public int StockId { get; set; }
        public int? ReferenceNo { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Suppliers? Suppliers { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Products? Products { get; set; }
        public int Units { get; set; }
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public Users? Users { get; set; }
        public int? LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Locations? Locations { get; set; }
        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Default value
    }
}
