using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketTracker.Models
{
    public class CreateTicketVm
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = "";

        [Required]
        public int CategoryId { get; set; }
    }
}
