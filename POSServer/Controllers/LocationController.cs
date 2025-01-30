using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace POSServer.Controllers
{
    [Route("api/locations")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
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
            var locations = _context.Locations.Where(l => l.LocationId == id).ToList();
            if (locations == null) return NotFound();
            return Ok(locations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Locations locations)
        {
            // Create a new Locations object with hashed password
            var location = new Locations
            {
                Name = locations.Name,
                Status = locations.Status,
                LocationType = locations.LocationType
            };

            // Add the new location to the context
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("LocationAdded", location);

            // Return the created location
            return CreatedAtAction(nameof(Get), new { id = location.LocationId }, location);
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

        [HttpPut("remove/{id}")]
        [Authorize]
        public async Task<IActionResult> Disable(int id)
        {
            var dbLocations = _context.Locations.Find(id);
            if (dbLocations == null) return NotFound();

            dbLocations.Status = 0;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("LocationUpdated", dbLocations);

            return NoContent();
        }
    }
}
