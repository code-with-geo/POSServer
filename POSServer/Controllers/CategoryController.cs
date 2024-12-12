﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace POSServer.Controllers
{
    [Route("api/category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<CategoryHub> _hubContext;

        public CategoryController(AppDbContext context, IHubContext<CategoryHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetAll()
        {
            var category = _context.Category.ToList();
            return Ok(category);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var category = _context.Category.Find(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Category category)
        {
            _context.Category.Add(category);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CategoryAdded", category);

            return CreatedAtAction(nameof(Get), new { id = category.CategoryId }, category);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Category category)
        {
            var dbCategory = _context.Products.Find(id);
            if (dbCategory == null) return NotFound();

            dbCategory.Name = category.Name;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CategoryUpdated", dbCategory);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var category = _context.Category.Find(id);
            if (category == null) return NotFound();

            _context.Category.Remove(category);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CategoryDeleted", id);

            return NoContent();
        }
    }
}