namespace POSServer.Models
{
    public class OrderRequest
    {
        public List<ProductOrderDetails?>? Products { get; set; }
        public int LocationId { get; set; } // Add this to specify the location
        public int UserId { get; set; } // Add this to specify the user
        public int DiscountId { get; set; } // Add this to specify the user
    }

    public class ProductOrderDetails
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } // You might want to add quantity
    }
}
