using System.Diagnostics;
using HelpdeskTicketTracker.Models;
using Microsoft.AspNetCore.Mvc;
using HelpdeskTicketTracker.Data;
using System.Text;
using System.Data;

namespace HelpdeskTicketTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public IActionResult DebugCategories()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new DataAccessLayer(dbPath);

            DataTable dt = dal.SelectData("SELECT CategoryId, Name FROM Categories ORDER BY CategoryId;");

            var sb = new StringBuilder();
            foreach (DataRow row in dt.Rows)
            {
                sb.AppendLine($"{row["CategoryId"]}: {row["Name"]}");
            }

            return Content(sb.ToString());
        }


        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
