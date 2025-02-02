using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class CashDrawer
    {
        public int DrawerId { get; set; }
        public string? Cashier { get; set; }
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public Users? Users { get; set; }

        public int? LocationId { get; set; }

        [ForeignKey("LocationId")]
        public Locations? Locations { get; set; }

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
        public DateTime TimeStart { get; set; } = DateTime.Now;
        public DateTime? TimeEnd { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Withdrawals> Withdrawal { get; set; } = new List<Withdrawals>();
        public ICollection<InitialCash> AdditionalInitialCash { get; set; } = new List<InitialCash>();

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalDiscount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalVatSale { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalVatAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalVatExempt { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalCashSales { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalEWalletSales { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalBankTransactionSales { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalCreditSales { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalSettledCredit { get; set; }

    }

    public class CashDrawerRequest
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public decimal InitialCash { get; set; }
    }
    public class EndCashDrawerRequest
    {
        public int DrawerId { get; set; }
    }
}
