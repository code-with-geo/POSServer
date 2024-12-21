using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace POSServer.Models
{
    public class OrderProducts
    {
        public int OrderId { get; set; }
        public Orders? Orders { get; set; }
        public int ProductId { get; set; }
        public Products? Products { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal SubTotal { get; set; }
    }
}
