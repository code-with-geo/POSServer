using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/locations")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<LocationHub> _hubContext;
        public LocationController(AppDbContext context, IHubContext<LocationHub> hubContext)
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

            var locations = _context.Locations.ToList();

            return Ok(locations);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var locations = _context.Locations.Find(id);
            if (locations == null) return NotFound();
            return Ok(locations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Locations locations)
        {
            _context.Locations.Add(locations);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("LocationAdded", locations);

            return CreatedAtAction(nameof(Get), new { id = locations.LocationId }, locations);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Locations locations)
        {
            var dbLocations = _context.Locations.Find(id);
            if (dbLocations == null) return NotFound();

            dbLocations.Name = locations.Name;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("LocationUpdated", dbLocations);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var locations = _context.Locations.Find(id);
            if (locations == null) return NotFound();

            _context.Locations.Remove(locations);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("LocationDeleted", id);

            return NoContent();
        }
    }
}
