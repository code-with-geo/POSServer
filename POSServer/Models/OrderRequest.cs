namespace POSServer.Models
{
    public class OrderRequest
    {
        public List<ProductOrderDetails?>? Products { get; set; }
        public int LocationId { get; set; } // Add this to specify the location
        public int UserId { get; set; } // Add this to specify the user
        public int CustomerId { get; set; } // Add this to specify the user
        public int TransactionType { get; set; }
        public int PaymentType { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public string? ReferenceNo { get; set; }
        public decimal DigitalPaymentAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalVatSale { get; set; }
        public decimal TotalVatAmount { get; set; }
        public decimal TotalVatExempt { get; set; }
        public int DiscountId { get; set; } // Add this to specify the user
    }

    public class ProductOrderDetails
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } // You might want to add quantity
        public int DiscountId { get; set; }
    }

    public class SettleRequest
    {
        public int LocationId { get; set; } // Add this to specify the location
        public int UserId { get; set; } // Add this to specify the user
        public string InvoiceNo { get; set; }
        public int PaymentType { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string ReferenceNo { get; set; }
        public decimal DigitalPaymentAmount { get; set; }
        public decimal TotalSettledCredit { get; set; }
    }
}
