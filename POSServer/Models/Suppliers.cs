using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Suppliers
    {
        public int SupplierId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
        public string? Name { get; set; }

        [Required]
        [StringLength(150, ErrorMessage = "Address cannot exceed 150 characters.")]
        public string? Address { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Contact person name cannot exceed 50 characters.")]
        public string? ContactPerson { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Contact no. cannot exceed 50 characters.")]
        public string? ContactNo { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }
    }
}
