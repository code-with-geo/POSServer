using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace POSServer.Controllers
{
    [Route("api/stock-in")]
    [ApiController]
    public class StockInController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<StockInHub> _hubContext;
        public StockInController(AppDbContext context, IHubContext<StockInHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(StockIn stockin)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                // Check if the ProductId exists in the Products table
                var productExists = await _context.Products.AnyAsync(p => p.Id == stockin.ProductId && p.Status == 1);
                if (!productExists)
                {
                    return NotFound(new
                    {
                        Message = "The specified product id does not exist or is inactive."
                    });
                }

                // Generate a random ReferenceNo
                var random = new Random();
                stockin.ReferenceNo = random.Next(100000, 999999); // Generates a 6-digit random number

                // Add the new StockIn entry
                _context.StockIn.Add(stockin);
                await _context.SaveChangesAsync();

                // Find the corresponding Inventory entry
                var existingInventory = await _context.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == stockin.ProductId && i.LocationId == stockin.LocationId);

                if (existingInventory != null)
                {
                    // Update the Units in the Inventory
                    existingInventory.Units += stockin.Units;
                    _context.Inventory.Update(existingInventory);
                }
                else
                {
                    // If no matching inventory exists, create a new one
                    var newInventory = new Inventory
                    {
                        ProductId = stockin.ProductId,
                        LocationId = stockin.LocationId,
                        Units = stockin.Units,
                        Status = 1, // Assuming active status
                        DateCreated = DateTime.UtcNow
                    };
                    _context.Inventory.Add(newInventory);
                }

                // Notify SignalR clients about the addition
                await _hubContext.Clients.All.SendAsync("StockInAdded", stockin);

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "StockIn added and inventory updated successfully.",
                    StockInId = stockin.StockId,
                    ReferenceNo = stockin.ReferenceNo
                });
            }
            catch (Exception ex)
            {
                // Rollback the transaction in case of an error
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing the request.",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("import")]
        [Authorize]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newStockIns = new List<StockIn>(); // To store newly added StockIns
                var updatedInventories = new List<Inventory>(); // To store updated inventories

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Assuming row 1 has headers
                        {
                            var userid = worksheet.Cells[row, 1].Value?.ToString();
                            var supplierId = worksheet.Cells[row, 2].Value?.ToString();
                            var productId = worksheet.Cells[row, 3].Value?.ToString();
                            var units = worksheet.Cells[row, 4].Value?.ToString();
                            var locationId = worksheet.Cells[row, 5].Value?.ToString();
                            var status = worksheet.Cells[row, 6].Value?.ToString();

                            if (int.TryParse(userid, out int parsedUserId) &&
                                int.TryParse(supplierId, out int parsedSupplierId) &&
                                int.TryParse(productId, out int parsedProductId) &&
                                int.TryParse(units, out int parsedUnits) &&
                                int.TryParse(locationId, out int parsedLocationId) &&
                                int.TryParse(status, out int parsedStatus))
                            {
                                // Check if the ProductId exists in the Products table
                                var productExists = await _context.Products.AnyAsync(p => p.Id == parsedProductId && p.Status == 1);
                                if (!productExists)
                                {
                                    return NotFound(new
                                    {
                                        Message = $"ProductId {parsedProductId} does not exist or is inactive."
                                    });
                                }

                                // Check if the SupplierId exists in the Suppliers table
                                var supplierExists = await _context.Suppliers.AnyAsync(s => s.SupplierId == parsedSupplierId);
                                if (!supplierExists)
                                {
                                    return NotFound(new
                                    {
                                        Message = $"SupplierId {parsedSupplierId} does not exist."
                                    });
                                }

                                // Generate a random ReferenceNo for the StockIn entry
                                var random = new Random();
                                var referenceNo = random.Next(100000, 999999); // Generates a 6-digit random number

                                // Add new StockIn entry
                                var newStockIn = new StockIn
                                {
                                    ProductId = parsedProductId,
                                    SupplierId = parsedSupplierId, // Assign SupplierId
                                    LocationId = parsedLocationId,
                                    Units = parsedUnits,
                                    ReferenceNo = referenceNo,
                                    UserId = parsedUserId, // Assign the UserId
                                    Status = parsedStatus,
                                    DateCreated = DateTime.UtcNow
                                };
                                newStockIns.Add(newStockIn);

                                // Check if Inventory exists
                                var existingInventory = await _context.Inventory
                                    .FirstOrDefaultAsync(i => i.ProductId == parsedProductId && i.LocationId == parsedLocationId);

                                if (existingInventory != null)
                                {
                                    // Update Inventory units
                                    existingInventory.Units += parsedUnits;
                                    updatedInventories.Add(existingInventory);
                                }
                                else
                                {
                                    // Create new Inventory entry
                                    var newInventory = new Inventory
                                    {
                                        ProductId = parsedProductId,
                                        LocationId = parsedLocationId,
                                        Units = parsedUnits,
                                        Status = 1, // Assuming active status
                                        DateCreated = DateTime.UtcNow
                                    };
                                    _context.Inventory.Add(newInventory);
                                }
                            }
                        }

                        // Save StockIn entries
                        if (newStockIns.Any())
                        {
                            _context.StockIn.AddRange(newStockIns);
                        }

                        // Save updated Inventory entries
                        if (updatedInventories.Any())
                        {
                            _context.Inventory.UpdateRange(updatedInventories);
                        }

                        // Save all changes
                        await _context.SaveChangesAsync();

                        // Notify SignalR clients about new StockIns
                        foreach (var stockIn in newStockIns)
                        {
                            await _hubContext.Clients.All.SendAsync("StockInAdded", stockIn);
                        }

                        // Commit transaction
                        await transaction.CommitAsync();
                    }
                }

                return Ok(new
                {
                    Message = "Excel data imported successfully.",
                    NewEntries = newStockIns.Count,
                    UpdatedInventories = updatedInventories.Count
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing the import.",
                    Error = ex.Message
                });
            }
        }
    }
}
