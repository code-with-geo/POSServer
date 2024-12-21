using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/suppliers")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<SupplierHub> _hubContext;

        public SupplierController(AppDbContext context, IHubContext<SupplierHub> hubContext)
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

            var suppliers = _context.Suppliers.ToList();

            return Ok(suppliers);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var suppliers = _context.Suppliers.Find(id);
            if (suppliers == null) return NotFound();
            return Ok(suppliers);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Suppliers suppliers)
        {
            _context.Suppliers.Add(suppliers);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("SupplierAdded", suppliers);

            return CreatedAtAction(nameof(Get), new { id = suppliers.SupplierId }, suppliers);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Suppliers suppliers)
        {
            var dbSupplier = _context.Suppliers.Find(id);
            if (dbSupplier == null) return NotFound();

            dbSupplier.Name = suppliers.Name;
            dbSupplier.Address = suppliers.Address;
            dbSupplier.ContactPerson = suppliers.ContactPerson;
            dbSupplier.ContactNo = suppliers.ContactNo;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("SupplierUpdated", dbSupplier);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var suppliers = _context.Suppliers.Find(id);
            if (suppliers == null) return NotFound();

            _context.Suppliers.Remove(suppliers);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("SupplierDeleted", id);

            return NoContent();
        }
    }
}
