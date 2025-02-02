using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Locations
    {
        public int LocationId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string? Name { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }

        public ICollection<Inventory> Inventory { get; set; } = new List<Inventory>();

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Location type must be 0 (inactive) or 1 (active).")]
        public int LocationType { get; set; }

        public ICollection<Users> Users { get; set; } = new List<Users>();

        public ICollection<Orders> Orders { get; set; } = new List<Orders>();

        public ICollection<StockIn> StockIn { get; set; } = new List<StockIn>();

        public ICollection<CashDrawer> CashDrawer { get; set; } = new List<CashDrawer>();

        public ICollection<StockAdjustments> StockAdjustments { get; set; } = new List<StockAdjustments>();

    }
}
