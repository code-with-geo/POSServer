namespace POSServer.Models
{
    public class OrderRequest
    {
        public List<ProductOrderDetails> Products { get; set; }
    }

    public class ProductOrderDetails
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } // You might want to add quantity
    }
}
