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
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<UserHub> _hubContext;

        public AuthController(AppDbContext context, IConfiguration configuration, IHubContext<UserHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(Users users)
        {
            if (await _context.Users.AnyAsync(u => u.Username == users.Username))
                return BadRequest("User already exists.");

            var user = new Users
            {
                Username = users.Username,
                Password = users.Password,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(users.Password),
                Name = users.Name,
                IsRole = users.IsRole,
                Status = users.Status
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var expirationTime = DateTime.UtcNow.AddMinutes(30);
            Console.WriteLine($"Token expiration time (UTC): {expirationTime}");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: expirationTime,
                signingCredentials: creds);

           

            return Ok(new
            {
                UserId = user.Id,
                LocationId = user.LocationId,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Message = "Login successfully"
            });
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetAll()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var users = _context.Users.ToList();

            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var user = _context.Users
              .Where(u => u.Id == id)
              .Select(u => new
              {
                  u.Id,
                  u.Username,
                  u.Password,
                  u.PasswordHash,
                  u.Name,
                  u.IsRole,
                  u.Status,
                  u.DateCreated
              })
              .FirstOrDefault();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Users users)
        {

            if (_context.Users.Any(u => u.Username == users.Username))
                return BadRequest("User already exists.");

            var user = new Users
            {
                Username = users.Username,
                Password = users.Password,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(users.Password),
                Name = users.Name,
                IsRole = users.IsRole,
                Status = users.Status,
                LocationId = users.LocationId
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("UserAdded", users);

            return CreatedAtAction(nameof(Get), new { id = users.Id }, users);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Users users)
        {
            var dbUser = _context.Users.Find(id);
            if (dbUser == null) return NotFound();

            dbUser.Username = users.Username;
            dbUser.Password = users.Password;
            dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(users.Password);
            dbUser.Name = users.Name;
            dbUser.IsRole = users.IsRole;
            dbUser.Status = users.Status;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("UserUpdated", dbUser);

            return NoContent();
        }

        [HttpPut("remove/{id}")]
        [Authorize]
        public async Task<IActionResult> Disable(int id)
        {
            var dbUser = _context.Users.Find(id);
            if (dbUser == null) return NotFound();

            dbUser.Status = 0;
            await _context.SaveChangesAsync();

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync("UserUpdated", dbUser);

            return NoContent();
        }

        [HttpGet("locations/{locationId}")]
        [Authorize]
        public IActionResult GetAll(int locationId)
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var users = _context.Users
                                .Where(u => u.LocationId == locationId) // Assuming your User model has a LocationId property
                                .ToList();

            if (!users.Any())
                return NotFound($"No users found for location ID {locationId}.");

            return Ok(users);
        }
    }
}
