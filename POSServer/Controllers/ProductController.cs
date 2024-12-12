using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ProductHub> _hubContext;

        public ProductController(AppDbContext context, IHubContext<ProductHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetAll()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var products = _context.Products
            .Include(p => p.Category)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Status,
                p.DateCreated,
                Category = p.Category == null
                    ? null
                    : new
                    {
                        p.Category.CategoryId,
                        p.Category.Name
                    }
            })
            .ToList();

            return Ok(products);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Products product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("ProductAdded", product);

            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Products product)
        {
            var dbProduct = _context.Products.Find(id);
            if (dbProduct == null) return NotFound();

            dbProduct.Name = product.Name;
            dbProduct.Description = product.Description;
            dbProduct.Price = product.Price;
            dbProduct.CategoryId = product.CategoryId;
            dbProduct.Status = product.Status;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("ProductUpdated", dbProduct);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("ProductDeleted", id);

            return NoContent();
        }
    }
}
