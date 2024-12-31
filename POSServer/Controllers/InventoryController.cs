﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<InventoryHub> _hubContext;
        public InventoryController(AppDbContext context, IHubContext<InventoryHub> hubContext)
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

            var inventory = _context.Inventory
            .Include(i => i.Products)
            .Include(i => i.Locations)
            .Select(i => new
            {
                i.InventoryId,
                i.Units,
                Product = i.Products == null ? null : new
                {
                    i.Products.Id,
                    i.Products.Name,
                    i.Products.Description,
                    i.Products.RetailPrice,
                    Category = i.Products.Category == null ? null : new
                    {
                        i.Products.Category.CategoryId,
                        i.Products.Category.Name
                    }
                },
                Location = i.Locations == null ? null : new
                {
                    i.Locations.LocationId,
                    i.Locations.Name
                },
                i.Status,
                i.DateCreated
            })
            .ToList();

            return Ok(inventory);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var inventory = _context.Inventory.Find(id);
            if (inventory == null) return NotFound();
            return Ok(inventory);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Inventory inventory)
        {
            // Check if the product already exists in the inventory
            var existingInventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == inventory.ProductId && i.LocationId == inventory.LocationId);

            if (existingInventory != null)
            {
                // Update the units for the existing product
                existingInventory.Units += inventory.Units;
                existingInventory.Specification = inventory.Specification;

                // Notify SignalR clients about the update
                await _hubContext.Clients.All.SendAsync("InventoryUpdated", existingInventory);
            }
            else
            {
                // Add a new inventory entry
                _context.Inventory.Add(inventory);

                // Notify SignalR clients about the addition
                await _hubContext.Clients.All.SendAsync("InventoryAdded", inventory);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = inventory.InventoryId }, inventory);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Inventory inventory)
        {
            var dbInventory = _context.Inventory.Find(id);
            if (dbInventory == null) return NotFound();

            dbInventory.Units = inventory.Units;
            dbInventory.ProductId = inventory.ProductId;
            dbInventory.LocationId = inventory.LocationId;
            dbInventory.Status = inventory.Status;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("InventoryUpdated", dbInventory);

            return NoContent();
        }

        [HttpPut("remove/{id}")]
        [Authorize]
        public async Task<IActionResult> Disable(int id)
        {
            var dbInventory = _context.Inventory.Find(id);
            if (dbInventory == null) return NotFound();

            dbInventory.Status = 0;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("InventoryUpdated", dbInventory);

            return NoContent();
        }


        [HttpGet("get/all")]
        [Authorize]
        public async Task<IActionResult> GetInventoryDetails()
        {
            var inventoryDetails = await (from inventory in _context.Inventory
                                          join product in _context.Products
                                          on inventory.ProductId equals product.Id
                                          join location in _context.Locations
                                          on inventory.LocationId equals location.LocationId
                                          select new
                                          {
                                              inventory.InventoryId,
                                              ProductName = product.Name,
                                              product.Barcode,
                                              ProductDescription = product.Description,
                                              inventory.Units,
                                              inventory.Specification,
                                              LocationName = location.Name,
                                              inventory.Status,
                                              inventory.DateCreated
                                          }).ToListAsync();

            return Ok(inventoryDetails);
        }
    }
}
