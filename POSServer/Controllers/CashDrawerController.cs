using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POSServer.Data;
using POSServer.Hubs;
using POSServer.Models;
using Microsoft.EntityFrameworkCore;

namespace POSServer.Controllers
{
    [Route("api/cashdrawer")]
    [ApiController]
    public class CashDrawerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<CashDrawerHub> _hubContext;
        public CashDrawerController(AppDbContext context, IHubContext<CashDrawerHub> hubContext)
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

            var drawers = _context.CashDrawer.ToList();

            return Ok(drawers);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var drawers = _context.CashDrawer.Find(id);
            if (drawers == null) return NotFound();
            return Ok(drawers);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CashDrawer drawers)
        {
            _context.CashDrawer.Add(drawers);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CashDrawerAdded", drawers);

            return CreatedAtAction(nameof(Get), new { id = drawers.DrawerId }, drawers);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, CashDrawer drawers)
        {
            var dbDrawer = _context.CashDrawer.Find(id);
            if (dbDrawer == null) return NotFound();

            dbDrawer.Cashier = drawers.Cashier;
            dbDrawer.InitialCash = drawers.InitialCash;
            dbDrawer.TotalSales = drawers.TotalSales;
            dbDrawer.Withdrawals = drawers.Withdrawals;
            dbDrawer.Expense = drawers.Expense;
            dbDrawer.DrawerCash = drawers.DrawerCash;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CashDrawerUpdated", dbDrawer);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var drawers = _context.CashDrawer.Find(id);
            if (drawers == null) return NotFound();

            _context.CashDrawer.Remove(drawers);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CashDrawerDeleted", id);

            return NoContent();
        }

    }


}
