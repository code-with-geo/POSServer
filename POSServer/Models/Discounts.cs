using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSServer.Models
{
    public class Discounts
    {
        public int DiscountId { get; set; }

        public string Name { get; set; }

        public int Percentage { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}
