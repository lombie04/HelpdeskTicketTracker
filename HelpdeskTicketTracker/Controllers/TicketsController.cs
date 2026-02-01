using HelpdeskTicketTracker.Data;
using HelpdeskTicketTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;

namespace HelpdeskTicketTracker.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        public IActionResult Index()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            DataTable dt = dal.SelectData(@"
SELECT t.TicketId, t.Title, t.Description, t.Status, t.CreatedAt, t.CategoryId, c.Name AS CategoryName
FROM Tickets t
JOIN Categories c ON c.CategoryId = t.CategoryId
ORDER BY t.TicketId DESC;
");

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

            return View(tickets);
        }
        [HttpGet]
        public IActionResult Create()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            DataTable dt = dal.SelectData("SELECT CategoryId, Name FROM Categories ORDER BY Name;");

            ViewBag.Categories = dt.Rows
                .Cast<DataRow>()
                .Select(r => new SelectListItem
                {
                    Value = r["CategoryId"].ToString(),
                    Text = r["Name"].ToString()
                })
                .ToList();

            return View(new CreateTicketVm());
        }

        [HttpPost]
        public IActionResult Create(CreateTicketVm vm)
        {
            if (!ModelState.IsValid)
            {
                var dbPath2 = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
                var dal2 = new DataAccessLayer(dbPath2);
                DataTable dt2 = dal2.SelectData("SELECT CategoryId, Name FROM Categories ORDER BY Name;");

                ViewBag.Categories = dt2.Rows
                    .Cast<DataRow>()
                    .Select(r => new SelectListItem
                    {
                        Value = r["CategoryId"].ToString(),
                        Text = r["Name"].ToString()
                    })
                    .ToList();

                return View(vm);
            }

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            dal.ExecuteNonQuery(@"
            INSERT INTO Tickets (Title, Description, Status, CreatedAt, CategoryId)
            VALUES (@title, @desc, 'Open', @created, @catId);
            ",
                ("@title", vm.Title),
                ("@desc", vm.Description),
                ("@created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                ("@catId", vm.CategoryId)
            );

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            DataTable tdt = dal.SelectData(@"
SELECT t.TicketId, t.Title, t.Description, t.Status, t.CreatedAt, t.CategoryId, c.Name AS CategoryName
FROM Tickets t
JOIN Categories c ON c.CategoryId = t.CategoryId
WHERE t.TicketId = @id;
", ("@id", id));

            if (tdt.Rows.Count == 0)
                return NotFound();

            var row = tdt.Rows[0];
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

        [HttpPost]
        public IActionResult AddComment(int id, string newCommentText)
        {
            if (string.IsNullOrWhiteSpace(newCommentText))
                return RedirectToAction("Details", new { id });

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

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
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            dal.ExecuteNonQuery("DELETE FROM Comments WHERE TicketId = @id;", ("@id", id));
            dal.ExecuteNonQuery("DELETE FROM Tickets WHERE TicketId = @id;", ("@id", id));

            return RedirectToAction("Index");
        }

    }
}
