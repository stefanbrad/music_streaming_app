using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Offstream.web.Data;
using Offstream.web.Models;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Offstream.Web.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private readonly OffstreamDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Injectăm baza de date și mediul (pentru a ști unde să salvăm fișierele fizic)
        public LibraryController(OffstreamDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sortBy = "recent")
        {
            // 1. Aflăm cine este logat
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            int currentUserId = int.Parse(userIdClaim);

            // 2. Căutăm în baza de date TOATE melodiile pe care le are acest user în librărie, cu sortare
            var query = _context.UserLibraries
                .Where(ul => ul.UserId == currentUserId)
                .Include(ul => ul.Song);

            IOrderedQueryable<UserLibrary> sortedQuery;
            if (sortBy == "alpha") //oerator ternar
            {
                sortedQuery = query.OrderBy(ul => ul.Song.Title);
            }  
            else
            {
                sortedQuery = query.OrderByDescending(ul => ul.DateAdded);
            }

            var mySongs = await sortedQuery
                .Select(ul => ul.Song)
                .ToListAsync();

            ViewBag.MyPlaylists = await _context.Playlists
                .Where(p => p.UserId == currentUserId)
                .ToListAsync();

            ViewBag.SortBy = sortBy;

            // 3. Trimitem lista către Front
            return View(mySongs);
        }

        // GET: /Library/Upload
        public IActionResult Upload()
        {
            return View(); // Afișează pagina de formular HTML desenată de Daria
        }

        // POST: /Library/Upload
      
        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> Upload(IFormFile file, string title, string artist)
        {
            // 1. Verificăm dacă fișierul a venit de la Daria (Front-End)
            if (file == null || file.Length == 0)
                return BadRequest("Nu a fost selectat niciun fișier.");

            // 2. Extragem ID-ul utilizatorului ÎN SIGURANȚĂ
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            int currentUserId = int.Parse(userIdClaim);

            // 3. Calculăm Hash-ul 
            string fileHash = CalculateHash(file);
            var existingSong = _context.Songs.FirstOrDefault(s => s.FileHash == fileHash);
            Song songToLink;

            // early return?                

            if (existingSong != null)
            {
                songToLink = existingSong;
            }
            else
            {
                // 4. Creăm folderul "music" ÎN SIGURANȚĂ absolută
                string webRootPath = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                string uploadsFolder = Path.Combine(webRootPath, "music");

                // Dacă folderul nu există, îl creează acum!
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = fileHash + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 5. Salvăm fișierul fizic
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                songToLink = new Song
                {
                    Title = title,
                    Artist = artist ?? "Unknown", // Evităm avertismentele de null
                    FileHash = fileHash,
                    FilePath = "/music/" + uniqueFileName
                };

                _context.Songs.Add(songToLink);
                await _context.SaveChangesAsync();
            }

            // 6. Legăm melodia de librăria userului
            bool isAlreadyInLibrary = _context.UserLibraries.Any(ul => ul.UserId == currentUserId && ul.SongId == songToLink.Id);
            if (!isAlreadyInLibrary)
            {
                var userLibraryEntry = new UserLibrary
                {
                    UserId = currentUserId,
                    SongId = songToLink.Id,
                    DateAdded = DateTime.UtcNow
                };
                _context.UserLibraries.Add(userLibraryEntry);
                await _context.SaveChangesAsync();
            }

            // 7. Gata! Ne întoarcem la librărie.
            return RedirectToAction("Index");
        }

        // Funcție ajutătoare privată pentru a calcula SHA-256
        private string CalculateHash(IFormFile file)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = file.OpenReadStream())
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                // Convertim biții într-un string lizibil (hexazecimal)
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}