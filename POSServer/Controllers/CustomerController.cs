using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<CustomerHub> _hubContext;

        public CustomerController(AppDbContext context, IHubContext<CustomerHub> hubContext)
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

            var customers = _context.Customers
                                    .Where(c => c.Status == 1) // Only get customers with Status = 1
                                    .ToList();

            return Ok(customers);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var customers = _context.Customers.Find(id);
            if (customers == null) return NotFound();
            return Ok(customers);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Customers customers)
        {
            _context.Customers.Add(customers);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CustomerAdded", customers);

            return CreatedAtAction(nameof(Get), new { id = customers.CustomerId }, customers);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Customers customers)
        {
            var dbCustomers = _context.Customers.Find(id);
            if (dbCustomers == null) return NotFound();

            dbCustomers.FirstName = customers.FirstName;
            dbCustomers.LastName = customers.LastName;
            dbCustomers.ContactNo = customers.ContactNo;
            dbCustomers.Email = customers.Email;

            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CustomerUpdated", dbCustomers);

            return NoContent();
        }
    }
}
