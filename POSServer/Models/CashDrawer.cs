using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class CashDrawer
    {
        public int DrawerId { get; set; }
        public string? Cashier { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Initial cash must be a positive value.")]
        public decimal InitialCash { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Total sales must be a positive value.")]
        public decimal TotalSales { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Withdrawals must be a positive value.")]
        public decimal Withdrawals { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Expense must be a positive value.")]
        public decimal Expense { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Drawer cash must be a positive value.")]
        public decimal DrawerCash { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime TimeStart { get; set; } = DateTime.UtcNow;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }
    }
}
