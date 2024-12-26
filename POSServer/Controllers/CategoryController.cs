using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;

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
            var dbCategory = _context.Category.Find(id);
            if (dbCategory == null) return NotFound();

            dbCategory.Name = category.Name;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CategoryUpdated", dbCategory);

            return NoContent();
        }

        [HttpPut("remove/{id}")]
        [Authorize]
        public async Task<IActionResult> Disable(int id)
        {
            var dbCategory = _context.Category.Find(id);
            if (dbCategory == null) return NotFound();

            dbCategory.Status = 0;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CategoryUpdated", dbCategory);

            return NoContent();
        }
    }
}
