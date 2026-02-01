namespace HelpdeskTicketTracker.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Open";
        public string CreatedAt { get; set; } = "";
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
    }
}
