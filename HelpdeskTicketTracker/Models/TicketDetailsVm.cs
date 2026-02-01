namespace HelpdeskTicketTracker.Models
{
    public class TicketDetailsVm
    {
        public Ticket Ticket { get; set; } = new Ticket();
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public string NewCommentText { get; set; } = "";
    }

    public class Comment
    {
        public int CommentId { get; set; }
        public int TicketId { get; set; }
        public string Text { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }
}
