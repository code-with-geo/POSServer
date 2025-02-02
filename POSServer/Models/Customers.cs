using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Customers
    {
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? FirstName { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? LastName { get; set; }
        [Required]
        [StringLength(11, ErrorMessage = "Contact no. cannot exceed 11 characters.")]
        public string? ContactNo { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "Email cannot exceed 50 characters.")]
        public string? Email { get; set; }
        public int TransactionCount { get; set; }
        public string? CardNumber { get; set; }
        public int Points { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        public ICollection<Orders> Orders { get; set; } = new List<Orders>();
    }
}
