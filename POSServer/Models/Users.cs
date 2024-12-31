using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? PasswordHash { get; set; }
        public string? Name { get; set; }

        [Required]
        [Range(0, 3, ErrorMessage = "Status must be 0 (admin), 1 (cashier), 2 (staff) or 3 (stock controller).")]
        public int IsRole { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        public ICollection<CashDrawer> CashDrawer { get; set; } = new List<CashDrawer>();

        public ICollection<Orders> Orders { get; set; } = new List<Orders>();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}
