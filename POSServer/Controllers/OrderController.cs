using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSServer.Data;
using POSServer.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using POSServer.Hubs;


namespace POSServer.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderController(AppDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Include(o => o.Location) // Include location details
                .Include(o => o.Users)    // Include user details
                .Select(order => new
                {
                    OrderId = order.OrderId,
                    DateCreated = order.DateCreated,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    User = new
                    {
                        UserId = order.Users != null ? order.Users.Id : (int?)null,
                        Name = order.Users != null ? order.Users.Name : null
                    },
                    Location = new
                    {
                        LocationId = order.Location.LocationId,
                        Name = order.Location.Name
                    },
                    Products = order.OrderProducts.Select(op => new
                    {
                        ProductId = op.ProductId,
                        ProductName = op.Products.Name,
                        RetailPrice = op.Products.RetailPrice,
                        Quantity = op.Quantity,
                        SubTotal = op.SubTotal
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] OrderRequest request)
        {
            if (request.Products == null || !request.Products.Any())
            {
                return BadRequest("Product list cannot be empty.");
            }

            // Verify if the location exists
            var location = await _context.Locations.FindAsync(request.LocationId);
            if (location == null)
            {
                return NotFound("Location not found.");
            }

            // Get product list from the database
            var productIds = request.Products.Select(p => p.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            if (products.Count != request.Products.Count)
            {
                return NotFound("One or more products not found.");
            }

            // Validate inventory for each product
            foreach (var productDetails in request.Products)
            {
                var inventory = await _context.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == productDetails.ProductId && i.LocationId == request.LocationId);

                if (inventory == null || inventory.Units < productDetails.Quantity)
                {
                    return BadRequest($"Not enough inventory for product ID {productDetails.ProductId}. Available: {inventory?.Units ?? 0}, Requested: {productDetails.Quantity}");
                }
            }

            // Get today's date (start and end range)
            var today = DateTime.Now.Date;

            // Count the number of orders for today
            int todaysOrderCount = await _context.Orders
                .Where(o => o.DateCreated >= today && o.DateCreated < today.AddDays(1))
                .CountAsync();

            // Generate the invoice number (format: OrderCount-yyyyMMdd)
            string invoiceNumber = $"INV{todaysOrderCount + 1}-{today:yyyyMMdd}";

            // Create the order
            var order = new Orders
            {
                InvoiceNo = invoiceNumber,
                Status = request.PaymentType == 3 ? 1 : 0,
                DateCreated = DateTime.UtcNow,
                TotalAmount = 0, // Will be calculated dynamically
                TotalDiscount = 0,
                TotalVatSale = request.TotalVatSale,
                TotalVatAmount = request.TotalVatAmount,
                TotalVatExempt = request.TotalVatExempt,
                TransactionType = request.TransactionType,
                PaymentType = request.PaymentType,
                LocationId = request.LocationId,
                UserId = request.UserId,
                CustomerId = request.CustomerId,
                AccountName = request.PaymentType == 1 || request.PaymentType ==  2 ? request.AccountName : "",
                AccountNumber = request.PaymentType == 1 || request.PaymentType == 2 ? request.AccountNumber : "",
                ReferenceNo = request.PaymentType == 1 || request.PaymentType == 2 ? request.ReferenceNo : "",
                DigitalPaymentAmount = request.PaymentType == 1 || request.PaymentType == 2 ? request.DigitalPaymentAmount : 0
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save to get OrderId

            decimal totalOrderAmount = 0;
            decimal totalDiscountAmount = 0;

            // Process each ordered product
            foreach (var productDetails in request.Products)
            {
                var product = products.FirstOrDefault(p => p.Id == productDetails.ProductId);
                if (product != null)
                {
                    // Fetch discount details for this product
                    var discount = await _context.Discounts
                        .FirstOrDefaultAsync(d => d.DiscountId == productDetails.DiscountId && d.Status == 1);

                    int discountPercentage = discount?.Percentage ?? 0;
                    decimal originalPrice = product.RetailPrice * productDetails.Quantity;
                    decimal discountAmount = (originalPrice * discountPercentage) / 100;
                    decimal subTotal = originalPrice - discountAmount;

                    // Accumulate order totals
                    totalOrderAmount += subTotal;
                    totalDiscountAmount += discountAmount;

                    // Add order-product entry
                    _context.OrderProducts.Add(new OrderProducts
                    {
                        OrderId = order.OrderId,
                        ProductId = product.Id,
                        Quantity = productDetails.Quantity,
                        DiscountId = productDetails.DiscountId,
                        SubTotal = subTotal
                    });

                    // Update inventory
                    var inventory = await _context.Inventory
                        .FirstOrDefaultAsync(i => i.ProductId == productDetails.ProductId && i.LocationId == request.LocationId);

                    if (inventory != null)
                    {
                        inventory.Units -= productDetails.Quantity;
                    }
                }
            }

            // Update order totals
            order.TotalAmount = totalOrderAmount;
            order.TotalDiscount = totalDiscountAmount;

            // Find the active cash drawer for this location and user
            var cashDrawer = await _context.CashDrawer
                .FirstOrDefaultAsync(cd => cd.LocationId == request.LocationId && cd.UserId == request.UserId && cd.Status == 1);

            if (cashDrawer != null)
            {
                // Update sales totals based on payment type
                switch (request.PaymentType)
                {
                    case 0:
                        cashDrawer.TotalCashSales += totalOrderAmount;
                        cashDrawer.DrawerCash += totalOrderAmount;
                        break;
                    case 1:
                        cashDrawer.TotalEWalletSales += totalOrderAmount;
                        break;
                    case 2:
                        cashDrawer.TotalBankTransactionSales += totalOrderAmount;
                        break;
                    case 3:
                        cashDrawer.TotalCreditSales += totalOrderAmount;
                        break;
                }

                // Update general totals
                cashDrawer.TotalAmount += totalOrderAmount;
                cashDrawer.TotalDiscount += totalDiscountAmount;
                cashDrawer.TotalVatSale += request.TotalVatSale;
                cashDrawer.TotalVatAmount += request.TotalVatAmount;
                cashDrawer.TotalVatExempt += request.TotalVatExempt;
                cashDrawer.TotalSales += totalOrderAmount;
            }
            else
            {
                return BadRequest("No active cash drawer found.");
            }

            // Save all changes
            await _context.SaveChangesAsync();

            // Now, update the customer’s transaction count and points
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);

            if (customer != null)
            {
                // Increment transaction count
                customer.TransactionCount++;

                // Calculate points: 1 point for every 200 in sales
                int earnedPoints = (int)(totalOrderAmount / 200);
                customer.Points += earnedPoints;

                // Save customer updates
                await _context.SaveChangesAsync();
            }

            // Notify all connected clients via SignalR
            await _hubContext.Clients.All.SendAsync("OrderCreated", new { OrderId = order.OrderId, Status = "Order created successfully" });

            return Ok(new
            {
                Status = "Order created successfully",
                InvoiceNo = invoiceNumber,
                TotalAmount = totalOrderAmount,
                TotalDiscount = totalDiscountAmount,
                TotalVatSale = request.TotalVatSale,
                TotalVatAmount = request.TotalVatAmount,
                TotalVatExempt = request.TotalVatExempt,
                PaymentType = request.PaymentType,
                AccountName = (request.PaymentType == 1 || request.PaymentType == 2) ? request.AccountName : "",
                AccountNumber = (request.PaymentType == 1 || request.PaymentType == 2) ? request.AccountNumber : "",
                ReferenceNo = (request.PaymentType == 1 || request.PaymentType == 2) ? request.ReferenceNo : "",
                DigitalPaymentAmount = (request.PaymentType == 1 || request.PaymentType == 2) ? request.DigitalPaymentAmount : 0
            });
        }

        [HttpPut("settle")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] SettleRequest request)
        {
            try
            {
                // Find the order by InvoiceNo
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.InvoiceNo == request.InvoiceNo);  // Using InvoiceNo to find the order

                if (order == null)
                {
                    return NotFound(new { Status = "Order not found" });
                }

                // Update OrderStatus
                order.Status = 0;

                // Conditionally update fields based on paymentType
                if (request.PaymentType == 1 || request.PaymentType == 2)
                {
                    order.AccountName = request.AccountName;
                    order.AccountNumber = request.AccountNumber;
                    order.ReferenceNo = request.ReferenceNo;
                    order.DigitalPaymentAmount = request.DigitalPaymentAmount;
                }
                else
                {
                    order.AccountName = string.Empty;
                    order.AccountNumber = string.Empty;
                    order.ReferenceNo = string.Empty;
                    order.DigitalPaymentAmount = 0;
                }

                // Find the active cash drawer for this location and user
                var cashDrawer = await _context.CashDrawer
                    .FirstOrDefaultAsync(cd => cd.LocationId == request.LocationId && cd.UserId == request.UserId && cd.Status == 1);

                if (cashDrawer != null)
                {
                    // Update the TotalSettledCredit
                    cashDrawer.TotalSettledCredit += request.TotalSettledCredit;

                    // Update sales totals based on payment type
                    switch (request.PaymentType)
                    {
                        case 0:
                            cashDrawer.DrawerCash += request.TotalSettledCredit;
                            break;
                    }
                }
                else
                {
                    return BadRequest(new { Status = "Cash drawer not found or not active" });
                }

                // Save changes
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Status = "Order updated successfully",
                    InvoiceNo = order.InvoiceNo,  // Returning InvoiceNo instead of OrderId
                    TotalSettledCredit = cashDrawer.TotalSettledCredit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }

        /*  [HttpPost]
          [Authorize]
          public async Task<IActionResult> Create([FromBody] OrderRequest request)
          {
              if (request.Products == null || !request.Products.Any())
              {
                  return BadRequest("Product list cannot be empty.");
              }

              // Verify if the location exists
              var location = await _context.Locations.FindAsync(request.LocationId);
              if (location == null)
              {
                  return NotFound("Location not found.");
              }

              // Get the list of products from the database
              var productIds = request.Products.Select(r => r.ProductId).ToList();
              var products = await _context.Products
                  .Where(p => productIds.Contains(p.Id))
                  .ToListAsync();

              if (products.Count != request.Products.Count)
              {
                  return NotFound("One or more products not found.");
              }

              // Get discount details
              var discount = await _context.Discounts
                  .FirstOrDefaultAsync(d => d.DiscountId == request.DiscountId && d.Status == 1);

              int discountPercentage = discount?.Percentage ?? 0; // Default to 0% if discount is null

              // Validate inventory for all products before proceeding
              foreach (var productDetails in request.Products)
              {
                  var product = products.FirstOrDefault(p => p.Id == productDetails.ProductId);
                  if (product != null)
                  {
                      // Check inventory
                      var inventory = await _context.Inventory
                          .FirstOrDefaultAsync(i => i.ProductId == productDetails.ProductId && i.LocationId == request.LocationId);

                      if (inventory == null)
                      {
                          return NotFound($"Inventory for product {product.Name} at location {location.Name} not found.");
                      }

                      if (inventory.Units < productDetails.Quantity)
                      {
                          return BadRequest($"Not enough inventory for product {product.Name}. Available: {inventory.Units}, Requested: {productDetails.Quantity}");
                      }
                  }
              }

              // Calculate total amount for the order
              decimal totalAmount = request.Products.Sum(productDetails =>
              {
                  var product = products.FirstOrDefault(p => p.Id == productDetails.ProductId);
                  if (product != null)
                  {
                      var originalPrice = product.RetailPrice * productDetails.Quantity;
                      var discountAmount = (originalPrice * discountPercentage) / 100;
                      return originalPrice - discountAmount;
                  }
                  return 0;
              });

              // Create the order
              var order = new Orders
              {
                  Status = 0, // Order status can be set here
                  DateCreated = DateTime.UtcNow, // Ensure to track when the order was created
                  TotalAmount = totalAmount, // Save the total amount
                  LocationId = request.LocationId, // Assign the location ID
                  UserId = request.UserId // Assign the user ID
              };

              _context.Orders.Add(order);
              await _context.SaveChangesAsync();

              // Add products to the OrderProducts table and update inventory
              foreach (var productDetails in request.Products)
              {
                  var product = products.FirstOrDefault(p => p.Id == productDetails.ProductId);
                  if (product != null)
                  {
                      var originalPrice = product.RetailPrice * productDetails.Quantity;
                      var discountAmount = (originalPrice * discountPercentage) / 100;
                      var subTotal = originalPrice - discountAmount;

                      _context.OrderProducts.Add(new OrderProducts
                      {
                          OrderId = order.OrderId,
                          ProductId = product.Id,
                          Quantity = productDetails.Quantity,
                          DiscountId = request.DiscountId,
                          SubTotal = subTotal // Save the discounted subtotal
                      });

                      // Update inventory
                      var inventory = await _context.Inventory
                          .FirstOrDefaultAsync(i => i.ProductId == productDetails.ProductId && i.LocationId == request.LocationId);

                      if (inventory != null)
                      {
                          inventory.Units -= productDetails.Quantity; // Deduct quantity from inventory
                      }
                  }
              }

              // Save changes to persist OrderProducts and inventory updates
              await _context.SaveChangesAsync();

              // Notify all connected clients via SignalR
              await _hubContext.Clients.All.SendAsync("OrderCreated", new { OrderId = order.OrderId, Status = "Order created successfully" });

              return Ok(new
              {
                  OrderId = order.OrderId,
                  Status = "Order created successfully",
                  TotalAmount = totalAmount,
                  DiscountPercentage = discountPercentage
              });
          }*/

        [HttpGet("credits")]
        [Authorize]
        public async Task<IActionResult> GetCreditOrders()
        {
            var orders = await _context.Orders
                .Where(o => o.LocationId == 1 && o.UserId == 2 && o.Status == 1)
                .Join(
                    _context.Customers,
                    order => order.CustomerId,
                    customer => customer.CustomerId,
                    (order, customer) => new
                    {
                        InvoiceNo = order.InvoiceNo,
                        TotalAmount = order.TotalAmount,
                        DateCreated = order.DateCreated,
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        ContactNo = customer.ContactNo,
                        Email = customer.Email,

                    }
                )
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("all/{locationId}")]
        [Authorize]
        public async Task<IActionResult> GetPendingOrders(int locationId, int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.LocationId == locationId && o.Status == 0)
                .Join(
                    _context.Customers,
                    order => order.CustomerId,
                    customer => customer.CustomerId,
                    (order, customer) => new
                    {
                        InvoiceNo = order.InvoiceNo,
                        TotalAmount = order.TotalAmount,
                        Name = customer.FirstName + " " + customer.LastName,
                        TransactionType = order.TransactionType == 1 ? "Retail Transaction" : "Wholesale",
                        PaymentType = order.PaymentType,
                        DateCreated = order.DateCreated
                    }
                )
                .ToListAsync();

            return Ok(orders);
        }
    }
}
