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
              .Select(order => new
              {
                  OrderId = order.OrderId,
                  DateCreated = order.DateCreated,
                  Status = order.Status,
                  TotalAmount = order.TotalAmount,
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

            // Get the list of products from the database
            var productIds = request.Products.Select(r => r.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != request.Products.Count)
            {
                return NotFound("One or more products not found.");
            }

            // Calculate total amount for the order
            decimal totalAmount = request.Products.Sum(productDetails =>
            {
                var product = products.FirstOrDefault(p => p.Id == productDetails.ProductId);
                return product != null ? product.RetailPrice * productDetails.Quantity : 0;
            });

            // Create the order
            var order = new Orders
            {
                Status = 0, // Order status can be set here
                DateCreated = DateTime.UtcNow, // Ensure to track when the order was created
                TotalAmount = totalAmount // Save the total amount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add products to the OrderProducts table
            foreach (var productDetails in request.Products)
            {
                var product = products.FirstOrDefault(p => p.Id == productDetails.ProductId);
                if (product != null)
                {
                    var subTotal = product.RetailPrice * productDetails.Quantity;
                    _context.OrderProducts.Add(new OrderProducts
                    {
                        OrderId = order.OrderId,
                        ProductId = product.Id,
                        Quantity = productDetails.Quantity,
                        SubTotal = subTotal // Save the subtotal
                    });
                }
            }

            // Save changes to persist OrderProducts
            await _context.SaveChangesAsync();


            // Notify all connected clients via SignalR
            await _hubContext.Clients.All.SendAsync("OrderCreated", new { OrderId = order.OrderId, Status = "Order created successfully" });

            return Ok(new { OrderId = order.OrderId, Status = "Order created successfully" });


        }

    }
}
