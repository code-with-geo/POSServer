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

        [HttpPost("start")]
        [Authorize]
        public async Task<IActionResult> StartCashDrawer([FromBody] CashDrawerRequest request)
        {
            if (request.InitialCash < 0)
            {
                return BadRequest("Initial cash must be a positive value.");
            }

            // Check if there's an existing open drawer for the same UserId and LocationId
            bool existingOpenDrawer = await _context.CashDrawer
                .AnyAsync(d => d.UserId == request.UserId && d.LocationId == request.LocationId && d.TimeEnd == null);

            if (existingOpenDrawer)
            {
                return BadRequest("A cash drawer is already open for this user and location. Close the existing drawer before starting a new one.");
            }

            var drawer = new CashDrawer
            {
                UserId = request.UserId,
                LocationId = request.LocationId,
                InitialCash = request.InitialCash,
                TotalSales = 0,
                Withdrawals = 0,
                Expense = 0,
                DrawerCash = request.InitialCash,
                TimeStart = DateTime.Now,
                TimeEnd = null, // Default to null
                DateCreated = DateTime.Now,
                Status = 1, // Active status
                TotalAmount = 0,
                TotalDiscount = 0,
                TotalVatSale = 0,
                TotalVatAmount = 0,
                TotalVatExempt = 0,
                TotalCashSales = 0,
                TotalEWalletSales = 0,
                TotalBankTransactionSales = 0,
                TotalCreditSales = 0,
                TotalSettledCredit = 0
            };

            _context.CashDrawer.Add(drawer);
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CashDrawerStarted", drawer);

            return CreatedAtAction(nameof(Get), new { id = drawer.DrawerId }, drawer);
        }

        [HttpPost("end")]
        [Authorize]
        public async Task<IActionResult> EndCashDrawer([FromBody] EndCashDrawerRequest request)
        {
            // Validate request to ensure DrawerId is valid
            if (request.DrawerId <= 0)
            {
                return BadRequest("Invalid DrawerId.");
            }

            // Find the related open CashDrawer entry
            var cashDrawer = await _context.CashDrawer
                .FirstOrDefaultAsync(d => d.DrawerId == request.DrawerId && d.TimeEnd == null);

            // Check if cash drawer is found and still open
            if (cashDrawer == null)
            {
                return BadRequest("Cash drawer not found or already closed.");
            }

            // Update the TimeEnd to close the drawer
            cashDrawer.TimeEnd = DateTime.Now;

            // Optional: Update the Status to indicate it's closed (if needed)
            cashDrawer.Status = 0; // Assuming 0 indicates closed status.

            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("CashDrawerClosed", cashDrawer);

            return Ok(new { message = "Cash drawer closed successfully.", cashDrawer });
        }

        [HttpPost("expense/add")]
        [Authorize]
        public async Task<IActionResult> AddExpense([FromBody] Expense request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Expense amount must be greater than zero.");
            }

            // Find the related CashDrawer entry
            var cashDrawer = await _context.CashDrawer
                .FirstOrDefaultAsync(d => d.DrawerId == request.DrawerId && d.TimeEnd == null);

            if (cashDrawer == null)
            {
                return BadRequest("Cash drawer not found or already closed.");
            }

            // Create a new expense entry
            var expenseEntry = new Expense
            {
                DrawerId = request.DrawerId,
                Description = request.Description,
                Amount = request.Amount,
                Remarks = request.Remarks
            };

            // Add to the Expense table (assuming there's a DbSet<Expense>)
            _context.Expense.Add(expenseEntry);

            // Update the CashDrawer's total expense and subtract from DrawerCash
            cashDrawer.Expense += request.Amount;
            cashDrawer.DrawerCash -= request.Amount; // Subtract expense from available cash

            await _context.SaveChangesAsync();

            // Notify SignalR clients (if needed)
            await _hubContext.Clients.All.SendAsync("ExpenseAdded", expenseEntry);

            return Ok(new { message = "Expense added successfully.", expense = expenseEntry });
        }

        [HttpPost("withdrawal/add")]
        [Authorize]
        public async Task<IActionResult> AddWithdrawal([FromBody] Withdrawals request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Withdrawal amount must be greater than zero.");
            }

            // Find the related CashDrawer entry
            var cashDrawer = await _context.CashDrawer
                .FirstOrDefaultAsync(d => d.DrawerId == request.DrawerId && d.TimeEnd == null);

            if (cashDrawer == null)
            {
                return BadRequest("Cash drawer not found or already closed.");
            }

            if (cashDrawer.DrawerCash < request.Amount)
            {
                return BadRequest("Insufficient funds in the cash drawer for this withdrawal.");
            }

            // Create a new withdrawal entry
            var withdrawalEntry = new Withdrawals
            {
                DrawerId = request.DrawerId,
                Description = request.Description,
                Amount = request.Amount,
                Remarks = request.Remarks,
                DateCreated = DateTime.Now
            };

            // Add to the Withdrawals table (assuming there's a DbSet<Withdrawals>)
            _context.Withdrawals.Add(withdrawalEntry);

            // Update the CashDrawer's total withdrawals and subtract from DrawerCash
            cashDrawer.Withdrawals += request.Amount;
            cashDrawer.DrawerCash -= request.Amount; // Subtract withdrawal from available cash

            await _context.SaveChangesAsync();

            // Notify SignalR clients (if needed)
            await _hubContext.Clients.All.SendAsync("WithdrawalAdded", withdrawalEntry);

            return Ok(new { message = "Withdrawal added successfully.", withdrawal = withdrawalEntry });
        }

        [HttpPost("initialcash/add")]
        [Authorize]
        public async Task<IActionResult> AddInitialCash([FromBody] InitialCash request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Initial cash amount must be greater than zero.");
            }

            // Find the related CashDrawer entry
            var cashDrawer = await _context.CashDrawer
                .FirstOrDefaultAsync(d => d.DrawerId == request.DrawerId && d.TimeEnd == null);

            if (cashDrawer == null)
            {
                return BadRequest("Cash drawer not found or already closed.");
            }

            // Create a new initial cash entry
            var initialCashEntry = new InitialCash
            {
                DrawerId = request.DrawerId,
                Description = request.Description,
                Amount = request.Amount,
                Remarks = request.Remarks
            };

            // Add to the InitialCash table
            _context.InitialCash.Add(initialCashEntry);

            // Update the CashDrawer's InitialCash and DrawerCash
            cashDrawer.InitialCash += request.Amount;
            cashDrawer.DrawerCash += request.Amount; // Add to available cash

            await _context.SaveChangesAsync();

            // Notify SignalR clients (if needed)
            await _hubContext.Clients.All.SendAsync("InitialCashAdded", initialCashEntry);

            return Ok(new { message = "Additional initial cash added successfully.", initialCash = initialCashEntry });
        }

        [HttpGet("ongoing/{userId}/{locationId}")]
        [Authorize]
        public async Task<IActionResult> GetOngoingCashDrawer(int userId, int locationId)
        {
            var ongoingDrawer = await _context.CashDrawer
                .FirstOrDefaultAsync(d => d.UserId == userId && d.LocationId == locationId && d.TimeEnd == null);

            if (ongoingDrawer == null)
            {
                return NotFound("No ongoing cash drawer found.");
            }

            return Ok(ongoingDrawer);
        }

    }


}
