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
        }

    }
}
