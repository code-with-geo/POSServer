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

        [Required]
        public int TransactionType { get; set; }

        [Required]
        public int PaymentType { get; set; }
        public string? InvoiceNo { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public string? ReferenceNo { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal DigitalPaymentAmount { get; set; }
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public Users? Users { get; set; }

        // Foreign key for Locations
        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Locations? Location { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customers? Customers { get; set; }

        // Many-to-many relationship with products
        public ICollection<OrderProducts> OrderProducts { get; set; } = new List<OrderProducts>();

    }
}
