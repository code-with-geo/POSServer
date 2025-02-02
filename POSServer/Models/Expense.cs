using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int DrawerId { get; set; }
        [ForeignKey("DrawerId")]
        public CashDrawer? CashDrawer { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Initial cash must be a positive value.")]
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.Now;


    }
}
