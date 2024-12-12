using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Products
    {
        public int Id { get; set; }


        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }

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
