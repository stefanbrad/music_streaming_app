using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Offstream.web.Data;
using Offstream.web.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Offstream.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly OffstreamDbContext _context;

        public AccountController(OffstreamDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string username, string password)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View();
            }

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password) // Nu salvăm parolele în clar!
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            // 1. Verificăm mai întâi dacă există Username-ul
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found. Try create a new account.");
                return View();
            }

            // 2. Dacă username-ul este bun, verificăm parola
            string hashedPw = HashPassword(password);
            if (user.PasswordHash != hashedPw)
            {
                ModelState.AddModelError("", "Incorrect password. Try again.");
                return View();
            }

            // 3.datele utilizatorului
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            // 4."Remember Me"
            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = rememberMe, // true dacă a bifat căsuța, false dacă nu
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            });

            return RedirectToAction("Index", "Library");
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login");
        }

        // Funcție ajutătoare pentru securitatea parolelor
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}