using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Threading.Tasks;

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
            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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
                p.Remarks,
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
            dbProduct.Remarks = product.Remarks;
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


        [HttpPost("import")]
        [Authorize]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var duplicateBarcodes = new List<string>();  // To store duplicate barcodes
                var newProducts = new List<Products>(); // To store newly added products

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Assuming row 1 has headers
                        {
                            var barcode = worksheet.Cells[row, 1].Value?.ToString();
                            var name = worksheet.Cells[row, 2].Value?.ToString();
                            var description = worksheet.Cells[row, 3].Value?.ToString();
                            var supplierPrice = worksheet.Cells[row, 4].Value?.ToString();
                            var retailPrice = worksheet.Cells[row, 5].Value?.ToString();
                            var wholesalePrice = worksheet.Cells[row, 6].Value?.ToString();
                            var reorderLevel = worksheet.Cells[row, 7].Value?.ToString();
                            var remarks = worksheet.Cells[row, 8].Value?.ToString();
                            var isVat = worksheet.Cells[row, 9].Value?.ToString();
                            var status = worksheet.Cells[row, 10].Value?.ToString();
                            var categoryId = worksheet.Cells[row, 11].Value?.ToString();

                            // Ensure required fields are valid
                            if (!string.IsNullOrEmpty(barcode) && !string.IsNullOrEmpty(name) &&
                                decimal.TryParse(supplierPrice, out decimal parsedSupplierPrice) &&
                                decimal.TryParse(retailPrice, out decimal parsedRetailPrice) &&
                                decimal.TryParse(wholesalePrice, out decimal parsedWholesalePrice) &&
                                int.TryParse(reorderLevel, out int parsedReorderLevel) &&
                                int.TryParse(isVat, out int parsedIsVat) &&
                                int.TryParse(status, out int parsedStatus))
                            {
                                // Check if the barcode already exists
                                var existingProduct = _context.Products.FirstOrDefault(p => p.Barcode == barcode);

                                if (existingProduct != null)
                                {
                                    // Update existing product with the same barcode
                                    existingProduct.Name = name;
                                    existingProduct.Description = description;
                                    existingProduct.SupplierPrice = parsedSupplierPrice;
                                    existingProduct.RetailPrice = parsedRetailPrice;
                                    existingProduct.WholesalePrice = parsedWholesalePrice;
                                    existingProduct.ReorderLevel = parsedReorderLevel;
                                    existingProduct.Remarks = remarks;
                                    existingProduct.IsVat = parsedIsVat;
                                    existingProduct.Status = parsedStatus;
                                    existingProduct.CategoryId = int.TryParse(categoryId, out int parsedCategoryId) ? parsedCategoryId : null;

                                    duplicateBarcodes.Add(barcode); // Add to duplicates list
                                }
                                else
                                {
                                    // If barcode is not a duplicate, create a new product
                                    var newProduct = new Products
                                    {
                                        Barcode = barcode,
                                        Name = name,
                                        Description = description,
                                        SupplierPrice = parsedSupplierPrice,
                                        RetailPrice = parsedRetailPrice,
                                        WholesalePrice = parsedWholesalePrice,
                                        ReorderLevel = parsedReorderLevel,
                                        Remarks = remarks,
                                        IsVat = parsedIsVat,
                                        Status = parsedStatus,
                                        CategoryId = int.TryParse(categoryId, out int parsedCategoryId) ? parsedCategoryId : null
                                    };

                                    newProducts.Add(newProduct);
                                }
                            }
                        }

                        // Save changes for new products and updated products
                        if (newProducts.Any())
                        {
                            _context.Products.AddRange(newProducts);
                            await _context.SaveChangesAsync();

                            // Notify SignalR clients about the new products
                            foreach (var newProduct in newProducts)
                            {
                                await _hubContext.Clients.All.SendAsync("ProductAdded", newProduct);
                            }
                        }

                        if (duplicateBarcodes.Any())
                        {
                            // Save changes for updated products (which are duplicates)
                            await _context.SaveChangesAsync();

                            // Notify SignalR clients about the updated products
                            foreach (var barcode in duplicateBarcodes)
                            {
                                var updatedProduct = _context.Products.FirstOrDefault(p => p.Barcode == barcode);
                                if (updatedProduct != null)
                                {
                                    await _hubContext.Clients.All.SendAsync("ProductUpdated", updatedProduct);
                                }
                            }
                        }
                    }
                }

                // Return response with success and duplicates if any
                if (duplicateBarcodes.Any())
                {
                    return Ok($"The following barcodes were updated: {string.Join(", ", duplicateBarcodes)}");
                }

                return Ok("Data imported and updated successfully.");
            }
            catch (DbUpdateException dbEx)
            {
                // Check for unique constraint violation
                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("duplicate"))
                {
                    return BadRequest("Duplicate barcode detected.");
                }

                // Catch any other database-related exceptions
                return StatusCode(500, $"Database error: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
