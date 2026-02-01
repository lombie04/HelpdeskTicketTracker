using HelpdeskTicketTracker.Data;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace HelpdeskTicketTracker.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            DataTable dt = dal.SelectData("SELECT CategoryId, Name FROM Categories ORDER BY Name;");
            return View(dt);
        }

        [HttpPost]
        public IActionResult Add(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return RedirectToAction("Index");

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            dal.ExecuteNonQuery(
                "INSERT OR IGNORE INTO Categories (Name) VALUES (@name);",
                ("@name", name.Trim())
            );

            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);
            var dt = dal.SelectData("SELECT COUNT(*) AS Cnt FROM Tickets WHERE CategoryId = @id;", ("@id", id));
            var cnt = Convert.ToInt32(dt.Rows[0]["Cnt"]);

            if (cnt > 0)
            {
                TempData["Error"] = "Cannot delete: category is used by existing tickets.";
                return RedirectToAction("Index");
            }

            dal.ExecuteNonQuery("DELETE FROM Categories WHERE CategoryId = @id;", ("@id", id));

            return RedirectToAction("Index");
        }

    }
}
