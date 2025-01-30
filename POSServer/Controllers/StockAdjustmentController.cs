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
    [Route("api/stock-adjustments")]
    [ApiController]
    public class StockAdjustmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<StockAdjustmentHub> _hubContext;
        public StockAdjustmentController(AppDbContext context, IHubContext<StockAdjustmentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AdjustInventory(StockAdjustments adjustment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate ProductId
                var productExists = await _context.Products.AnyAsync(p => p.Id == adjustment.ProductId && p.Status == 1);
                if (!productExists)
                {
                    return NotFound(new
                    {
                        Message = "The specified product id does not exist or is inactive."
                    });
                }

                // Find the corresponding Inventory entry
                var existingInventory = await _context.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == adjustment.ProductId && i.LocationId == adjustment.LocationId);

                if (existingInventory == null)
                {
                    return NotFound(new
                    {
                        Message = "No inventory found for the specified product and location."
                    });
                }

                // Perform action based on Actions property
                if (adjustment.Actions == 0) // Add to inventory
                {
                    existingInventory.Units += adjustment.Units;
                }
                else if (adjustment.Actions == 1) // Subtract from inventory
                {
                    if (existingInventory.Units < adjustment.Units)
                    {
                        return BadRequest(new
                        {
                            Message = "Cannot subtract more units than are available in inventory."
                        });
                    }

                    existingInventory.Units -= adjustment.Units;
                }
                else
                {
                    return BadRequest(new
                    {
                        Message = "Invalid action. Actions must be 0 (add) or 1 (remove)."
                    });
                }

                // Save the adjustment record
                adjustment.DateCreated = DateTime.UtcNow;
                _context.StockAdjustments.Add(adjustment);

                // Update the inventory
                _context.Inventory.Update(existingInventory);

                // Save changes
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Inventory adjustment completed successfully.",
                    AdjustmentId = adjustment.AdjustmentId,
                    UpdatedInventory = new
                    {
                        ProductId = existingInventory.ProductId,
                        LocationId = existingInventory.LocationId,
                        CurrentUnits = existingInventory.Units
                    }
                });
            }
            catch (Exception ex)
            {
                // Rollback the transaction in case of an error
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing the inventory adjustment.",
                    Error = ex.Message
                });
            }
        }
        [HttpPost("import")]
        [Authorize]
        public async Task<IActionResult> ImportStockAdjustments(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    Message = "No file uploaded."
                });
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Load the Excel file
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);

                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                // Validate the data in the Excel file
                for (int row = 2; row <= rowCount; row++) // Start from 2 assuming the first row contains headers
                {
                    var productId = worksheet.Cells[row, 1].GetValue<int>();
                    var units = worksheet.Cells[row, 2].GetValue<int>();
                    var reason = worksheet.Cells[row, 3].GetValue<string>();
                    var userId = worksheet.Cells[row, 4].GetValue<int>();
                    var locationId = worksheet.Cells[row, 5].GetValue<int>();
                    var actions = worksheet.Cells[row, 6].GetValue<int>();

                    // Validate the product
                    var productExists = await _context.Products.AnyAsync(p => p.Id == productId && p.Status == 1);
                    if (!productExists)
                    {
                        return BadRequest(new
                        {
                            Message = $"Invalid or inactive product ID at row {row}."
                        });
                    }

                    // Find the existing inventory entry
                    var existingInventory = await _context.Inventory
                        .FirstOrDefaultAsync(i => i.ProductId == productId && i.LocationId == locationId);

                    if (existingInventory == null)
                    {
                        return BadRequest(new
                        {
                            Message = $"No inventory found for product ID {productId} at location {locationId} at row {row}."
                        });
                    }

                    // Perform action based on Actions property
                    if (actions == 0) // Add to inventory
                    {
                        existingInventory.Units += units;
                    }
                    else if (actions == 1) // Subtract from inventory
                    {
                        if (existingInventory.Units < units)
                        {
                            return BadRequest(new
                            {
                                Message = $"Cannot subtract more units than are available in inventory at row {row}."
                            });
                        }

                        existingInventory.Units -= units;
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            Message = $"Invalid action (must be 0 or 1) at row {row}."
                        });
                    }

                    // Create adjustment record
                    var adjustment = new StockAdjustments
                    {
                        ProductId = productId,
                        Units = units,
                        Reason = reason,
                        UserId = userId,
                        LocationId = locationId,
                        Actions = actions,
                        DateCreated = DateTime.UtcNow
                    };
                    _context.StockAdjustments.Add(adjustment);

                    // Update the inventory
                    _context.Inventory.Update(existingInventory);
                }

                // Save all changes in a single transaction
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Excel file processed successfully and inventory updated."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing the Excel file.",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetStockAdjustments()
        {
            try
            {
                // Perform the join query using LINQ
                var adjustments = await (from a in _context.StockAdjustments
                                         join p in _context.Products on a.ProductId equals p.Id
                                         join u in _context.Users on a.UserId equals u.Id
                                         join l in _context.Locations on a.LocationId equals l.LocationId
                                         select new
                                         {
                                             AdjustmentId = a.AdjustmentId,
                                             ProductName = p.Name,
                                             a.Units,
                                             a.Reason,
                                             UserName = u.Name,
                                             LocationName = l.Name,
                                             a.Actions
                                         }).ToListAsync();

                // Check if adjustments were found
                if (adjustments == null || adjustments.Count == 0)
                {
                    return NotFound(new
                    {
                        Message = "No stock adjustments found."
                    });
                }

                // Return the adjustments data
                return Ok(new
                {
                    Message = "Stock adjustments retrieved successfully.",
                    Data = adjustments
                });
            }
            catch (Exception ex)
            {
                // Handle error
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving stock adjustments.",
                    Error = ex.Message
                });
            }
        }
    }
}
