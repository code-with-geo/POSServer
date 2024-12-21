using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/discounts")]
    [ApiController]
    public class DiscountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DiscountHub> _hubContext;

        public DiscountController(AppDbContext context, IHubContext<DiscountHub> hubContext)
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

            var discounts = _context.Discounts.ToList();

            return Ok(discounts);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var discounts = _context.Discounts.Find(id);
            if (discounts == null) return NotFound();
            return Ok(discounts);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Discounts discounts)
        {
            _context.Discounts.Add(discounts);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("DiscountAdded", discounts);

            return CreatedAtAction(nameof(Get), new { id = discounts.DiscountId }, discounts);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Discounts discounts)
        {
            var dbDiscount = _context.Discounts.Find(id);
            if (dbDiscount == null) return NotFound();

            dbDiscount.Name = discounts.Name;
            dbDiscount.Percentage = discounts.Percentage;
            dbDiscount.Status = discounts.Status;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("DiscountUpdated", dbDiscount);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var discounts = _context.Discounts.Find(id);
            if (discounts == null) return NotFound();

            _context.Discounts.Remove(discounts);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("DiscountDeleted", id);

            return NoContent();
        }
    }
}
