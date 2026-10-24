using Microsoft.AspNetCore.Mvc;
using Offstream.web.Data;
using System.IO;

namespace Offstream.Web.Controllers
{
    public class AudioController : Controller
    {
        private readonly OffstreamDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AudioController(OffstreamDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Audio/Stream/{id}
        [HttpGet]
        public IActionResult Stream(int id)
        {
            // 1. Căutăm melodia în baza de date după Id
            var song = _context.Songs.FirstOrDefault(s => s.Id == id);

            if (song == null)
            {
                return NotFound("The song has not been found.");
            }

            // 2. Construim calea fizică absolută către fișierul audio de pe server
            // song.FilePath este salvat în formatul "/music/hash-ul.mp3" (vezi LibraryController)
            // Trebuie să scoatem primul slash "/" ca să îl putem combina corect cu folderul wwwroot
            string relativePath = song.FilePath.TrimStart('/');
            string physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            // 3. Verificăm dacă fișierul chiar există pe hard disk (siguranță în plus)
            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("Fișierul fizic lipsește de pe server.");
            }

            // 4. Returnăm fișierul audio cu funcția de "Buffering" activată!
            // Setarea 'enableRangeProcessing: true' este secretul care permite player-ului HTML5
            // să deruleze melodia și să nu descarce tot fișierul deodată.
            return PhysicalFile(physicalPath, "audio/mpeg", enableRangeProcessing: true);
        }
    }
}