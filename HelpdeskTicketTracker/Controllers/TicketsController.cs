using HelpdeskTicketTracker.Data;
using HelpdeskTicketTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace HelpdeskTicketTracker.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private DataAccessLayer GetDal()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            return new DataAccessLayer(dbPath);
        }

        // ADMIN + AGENT: can see all tickets (or filtered for agent)
        [Authorize(Roles = "Admin,Agent")]
        public IActionResult Index()
        {
            var dal = GetDal();

            // If you want Agent to see only Open/In Progress, uncomment the WHERE line
            DataTable dt = dal.SelectData(@"
SELECT t.TicketId, t.Title, t.Description, t.Status, t.CreatedAt, t.CategoryId, t.CreatedBy,
       c.Name AS CategoryName
FROM Tickets t
JOIN Categories c ON c.CategoryId = t.CategoryId
-- WHERE t.Status <> 'Closed'
ORDER BY t.TicketId DESC;
");

            return View(MapTickets(dt));
        }

        // USER: can see only their own tickets
        [Authorize(Roles = "User")]
        public IActionResult My()
        {
            var username = User?.Identity?.Name ?? "";
            var dal = GetDal();

            DataTable dt = dal.SelectData(@"
SELECT t.TicketId, t.Title, t.Description, t.Status, t.CreatedAt, t.CategoryId, t.CreatedBy,
       c.Name AS CategoryName
FROM Tickets t
JOIN Categories c ON c.CategoryId = t.CategoryId
WHERE t.CreatedBy = @u
ORDER BY t.TicketId DESC;
", ("@u", username));

            // reuse the same Index view so you don't create a new cshtml
            return View("Index", MapTickets(dt));
        }

        // USER + ADMIN: can create tickets
        [Authorize(Roles = "User,Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            var dal = GetDal();
            LoadCategories(dal);
            return View(new CreateTicketVm());
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult Create(CreateTicketVm vm)
        {
            var dal = GetDal();

            if (!ModelState.IsValid)
            {
                LoadCategories(dal);
                return View(vm);
            }

            var username = User?.Identity?.Name ?? "unknown";

            dal.ExecuteNonQuery(@"
INSERT INTO Tickets (Title, Description, Status, CreatedAt, CategoryId, CreatedBy)
VALUES (@title, @desc, 'Open', @created, @catId, @createdBy);
",
                ("@title", vm.Title),
                ("@desc", vm.Description),
                ("@created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                ("@catId", vm.CategoryId),
                ("@createdBy", username)
            );

            // User should go to My tickets, Admin can go to Index
            if (User.IsInRole("User"))
                return RedirectToAction("My");

            return RedirectToAction("Index");
        }

        // DETAILS: User can only open their own ticket
        [HttpGet]
        public IActionResult Details(int id)
        {
            var dal = GetDal();

            DataTable tdt = dal.SelectData(@"
SELECT t.TicketId, t.Title, t.Description, t.Status, t.CreatedAt, t.CategoryId, t.CreatedBy,
       c.Name AS CategoryName
FROM Tickets t
JOIN Categories c ON c.CategoryId = t.CategoryId
WHERE t.TicketId = @id;
", ("@id", id));

            if (tdt.Rows.Count == 0)
                return NotFound();

            var row = tdt.Rows[0];

            // If logged in as User, block access to other people's tickets
            var createdBy = row["CreatedBy"]?.ToString() ?? "";
            var username = User?.Identity?.Name ?? "";

            if (User.IsInRole("User") && !string.Equals(createdBy, username, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var ticket = new Ticket
            {
                TicketId = Convert.ToInt32(row["TicketId"]),
                Title = row["Title"].ToString() ?? "",
                Description = row["Description"].ToString() ?? "",
                Status = row["Status"].ToString() ?? "",
                CreatedAt = row["CreatedAt"].ToString() ?? "",
                CategoryId = Convert.ToInt32(row["CategoryId"]),
                CategoryName = row["CategoryName"].ToString() ?? ""
            };

            DataTable cdt = dal.SelectData(@"
SELECT CommentId, TicketId, Text, CreatedAt
FROM Comments
WHERE TicketId = @id
ORDER BY CommentId DESC;
", ("@id", id));

            var comments = new List<Comment>();
            foreach (DataRow r in cdt.Rows)
            {
                comments.Add(new Comment
                {
                    CommentId = Convert.ToInt32(r["CommentId"]),
                    TicketId = Convert.ToInt32(r["TicketId"]),
                    Text = r["Text"].ToString() ?? "",
                    CreatedAt = r["CreatedAt"].ToString() ?? ""
                });
            }

            var vm = new TicketDetailsVm
            {
                Ticket = ticket,
                Comments = comments
            };

            return View(vm);
        }

        // Anyone logged in can comment, but still respect "User can comment only on their own ticket"
        [HttpPost]
        public IActionResult AddComment(int id, string newCommentText)
        {
            if (string.IsNullOrWhiteSpace(newCommentText))
                return RedirectToAction("Details", new { id });

            // Ensure user isn't commenting on someone else's ticket
            if (User.IsInRole("User"))
            {
                var dalCheck = GetDal();
                var username = User?.Identity?.Name ?? "";
                var check = dalCheck.SelectData("SELECT CreatedBy FROM Tickets WHERE TicketId = @id;", ("@id", id));
                if (check.Rows.Count == 0) return NotFound();

                var createdBy = check.Rows[0]["CreatedBy"]?.ToString() ?? "";
                if (!string.Equals(createdBy, username, StringComparison.OrdinalIgnoreCase))
                    return Forbid();
            }

            var dal = GetDal();
            dal.ExecuteNonQuery(@"
INSERT INTO Comments (TicketId, Text, CreatedAt)
VALUES (@tid, @text, @created);
",
                ("@tid", id),
                ("@text", newCommentText.Trim()),
                ("@created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            );

            return RedirectToAction("Details", new { id });
        }


        [Authorize(Roles = "Admin,Agent")]
        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var allowed = new[] { "Open", "In Progress", "Closed" };
            if (!allowed.Contains(status))
                return RedirectToAction("Details", new { id });

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            dal.ExecuteNonQuery(
                "UPDATE Tickets SET Status = @status WHERE TicketId = @id;",
                ("@status", status),
                ("@id", id)
            );

            return RedirectToAction("Details", new { id });
        }

        private void LoadCategories(DataAccessLayer dal)
        {
            DataTable dt = dal.SelectData("SELECT CategoryId, Name FROM Categories ORDER BY Name;");

            ViewBag.Categories = dt.Rows
                .Cast<DataRow>()
                .Select(r => new SelectListItem
                {
                    Value = r["CategoryId"].ToString(),
                    Text = r["Name"].ToString()
                })
                .ToList();
        }

        private List<Ticket> MapTickets(DataTable dt)
        {
            var tickets = new List<Ticket>();
            foreach (DataRow row in dt.Rows)
            {
                tickets.Add(new Ticket
                {
                    TicketId = Convert.ToInt32(row["TicketId"]),
                    Title = row["Title"].ToString() ?? "",
                    Description = row["Description"].ToString() ?? "",
                    Status = row["Status"].ToString() ?? "",
                    CreatedAt = row["CreatedAt"].ToString() ?? "",
                    CategoryId = Convert.ToInt32(row["CategoryId"]),
                    CategoryName = row["CategoryName"].ToString() ?? ""
                });
            }
            return tickets;
        }
    }
}
