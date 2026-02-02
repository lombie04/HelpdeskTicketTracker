using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;

namespace HelpdeskTicketTracker.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter username and password.";
                return View();
            }

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "helpdesk.db");
            var dal = new HelpdeskTicketTracker.Data.DataAccessLayer(dbPath);

            var dt = dal.ExecuteQuery(
                "SELECT Username, Role FROM Users WHERE Username = @u AND PasswordHash = @p LIMIT 1;",
                ("@u", username.Trim()),
                ("@p", password)
            );

            if (dt.Rows.Count == 1)
            {
                var role = dt.Rows[0]["Role"]?.ToString() ?? "User";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username.Trim()),
                    new Claim(ClaimTypes.Role, role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal
                );

                return role switch
                {
                    "Admin" => RedirectToAction("Index", "Admin"),
                    "Agent" => RedirectToAction("Index", "Agent"),
                    _ => RedirectToAction("Index", "User")
                };
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
