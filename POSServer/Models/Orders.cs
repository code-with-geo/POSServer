using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Orders
    {
        public int OrderId { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        public int Status { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal TotalAmount { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public Users? Users { get; set; }

        // Foreign key for Locations
        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Locations? Location { get; set; }

        // Foreign key for Discounts
        public int DiscountId { get; set; }
        [ForeignKey("DiscountId")]
        public Discounts? Discounts { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customers? Customers { get; set; }

        // Many-to-many relationship with products
        public ICollection<OrderProducts> OrderProducts { get; set; } = new List<OrderProducts>();

    }
}
