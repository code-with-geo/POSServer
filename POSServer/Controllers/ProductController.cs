using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
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
                p.Barcode,
                p.Name,
                p.Description,
                p.SupplierPrice,
                p.RetailPrice,
                p.WholesalePrice,
                p.ReorderLevel,
                p.IsVat,
                p.Status,
                p.DateCreated,
                p.CategoryId,
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

            dbProduct.Barcode = product.Barcode;
            dbProduct.Name = product.Name;
            dbProduct.Description = product.Description;
            dbProduct.SupplierPrice = product.SupplierPrice;
            dbProduct.RetailPrice = product.RetailPrice;
            dbProduct.WholesalePrice = product.WholesalePrice;
            dbProduct.CategoryId = product.CategoryId;
            dbProduct.Status = product.Status;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("ProductUpdated", dbProduct);

            return NoContent();
        }

        [HttpPut("remove/{id}")]
        [Authorize]
        public async Task<IActionResult> Disable(int id)
        {
            var dbProduct = _context.Products.Find(id);
            if (dbProduct == null) return NotFound();

            dbProduct.Status = 0;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("ProductUpdated", dbProduct);

            return NoContent();
        }

        [HttpGet("barcode/{barcode}")]
        [Authorize]
        public IActionResult GetByBarcode(string barcode)
        {
            // Find the product with the specified barcode
            var product = _context.Products.FirstOrDefault(p => p.Barcode == barcode);

            // Return 404 if the product is not found
            if (product == null) return NotFound();

            // Return the product if found
            return Ok(product);
        }

        [HttpGet("active/all")]
        [Authorize]
        public IActionResult GetAllActive()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            // Retrieve products with Status = 1
            var products = _context.Products
                .Where(p => p.Status == 1) // Filter products with Status = 1
                .Include(p => p.Category) // Include the related Category
                .Select(p => new
                {
                    p.Id,
                    p.Barcode,
                    p.Name,
                    p.Description,
                    p.SupplierPrice,
                    p.RetailPrice,
                    p.WholesalePrice,
                    p.ReorderLevel,
                    p.IsVat,
                    p.Status,
                    p.DateCreated,
                    p.CategoryId,
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
    }
}
