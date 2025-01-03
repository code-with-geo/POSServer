﻿using Microsoft.AspNetCore.Authorization;
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


            var drawers = _context.CashDrawer
           .Include(u => u.Users)
           .Select(d => new
           {
               d.DrawerId,
               d.Cashier,
               d.InitialCash,
               d.TotalSales,
               d.Withdrawals,
               d.Expense,
               d.DrawerCash,
               d.TimeStart,
               d.DateCreated,
               d.Status,
               d.UserId,
               Users = d.Users == null
                   ? null
                   : new
                   {
                       d.Users.Id,
                       d.Users.Name
                   }
           })
           .ToList();

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
            dbDrawer.UserId = drawers.UserId;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CashDrawerUpdated", dbDrawer);

            return NoContent();
        }


        [HttpPut("remove/{id}")]
        [Authorize]
        public async Task<IActionResult> Disable(int id)
        {
            var dbDrawer = _context.CashDrawer.Find(id);
            if (dbDrawer == null) return NotFound();

            dbDrawer.Status = 0;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("SupplierUpdated", dbDrawer);

            return NoContent();
        }

    }


}
