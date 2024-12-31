using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Products
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
        public string? Barcode { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Suppler price must be a positive value.")]
        public decimal SupplierPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Retail price must be a positive value.")]
        public decimal RetailPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Wholesale price must be a positive value.")]
        public decimal WholesalePrice { get; set; }
        public int ReorderLevel { get; set; }

        [Required]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        public string? Remarks { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "IsVat must be 0 (inactive) or 1 (active).")]
        public int IsVat { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Default value

        //Category
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        //Inventory
        public ICollection<Inventory> Inventory { get; set; } = new List<Inventory>();

        // Many-to-many relationship with orders
        public ICollection<OrderProducts> OrderProducts { get; set; } = new List<OrderProducts>();

    }
}
