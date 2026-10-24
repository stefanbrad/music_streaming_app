using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Offstream.web.Data;
using Offstream.web.Models;
using System.Security.Claims;
using System.IO;

namespace Offstream.web.Controllers
{
    [Authorize]
    public class PlaylistController : Controller
    {
        private readonly OffstreamDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PlaylistController(OffstreamDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Afișează playlist-urile
        [HttpGet]
        public async Task<IActionResult> Index(string sortBy = "recent")
        {
            int currentUserId = GetCurrentUserId();

            // 1. My owned playlists
            var query = _context.Playlists
                .Where(p => p.UserId == currentUserId);

            IOrderedQueryable<Playlist> sortedQuery;
            if (sortBy == "alpha")
            {
                sortedQuery = query.OrderBy(p => p.Name);
            }
            else
            {
                sortedQuery = query.OrderByDescending(p => p.Id);
            }
            var myPlaylists = await sortedQuery.ToListAsync();

            // 2. Saved playlists created by other users
            var savedPlaylistsQuery = _context.SavedPlaylists
                .Where(sp => sp.UserId == currentUserId)
                .Include(sp => sp.Playlist)
                .ThenInclude(p => p.User)
                .Select(sp => sp.Playlist);

            List<Playlist> savedPlaylists;
            if (sortBy == "alpha")
            {
                savedPlaylists = await savedPlaylistsQuery.OrderBy(p => p.Name).ToListAsync();
            }
            else
            {
                savedPlaylists = await savedPlaylistsQuery.OrderByDescending(p => p.Id).ToListAsync();
            }

            // 3. Explore public/shared playlists by other users
            var publicPlaylistsQuery = _context.Playlists
                .Where(p => p.IsShared && p.UserId != currentUserId)
                .Include(p => p.User);

            List<Playlist> publicPlaylists;
            if (sortBy == "alpha")
            {
                publicPlaylists = await publicPlaylistsQuery.OrderBy(p => p.Name).ToListAsync();
            }
            else
            {
                publicPlaylists = await publicPlaylistsQuery.OrderByDescending(p => p.Id).ToListAsync();
            }

            ViewBag.SortBy = sortBy;
            ViewBag.SavedPlaylists = savedPlaylists;
            ViewBag.PublicPlaylists = publicPlaylists;

            return View(myPlaylists);
        }

        // POST: Creează un playlist nou
        [HttpPost]
        public async Task<IActionResult> Create(string name, bool isShared)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            int currentUserId = GetCurrentUserId();

            var newPlaylist = new Playlist
            {
                Name = name,
                UserId = currentUserId,
                IsShared = isShared
            };

            _context.Playlists.Add(newPlaylist);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // POST: Editează un playlist (redenumire & copertă)
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string name, bool isShared, IFormFile? coverFile)
        {
            int currentUserId = GetCurrentUserId();
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

            if (playlist == null)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Details", new { id = id });
            }

            playlist.Name = name;
            playlist.IsShared = isShared;

            if (coverFile != null && coverFile.Length > 0)
            {
                string webRootPath = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                string coversFolder = Path.Combine(webRootPath, "covers");
                if (!Directory.Exists(coversFolder))
                {
                    Directory.CreateDirectory(coversFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(coverFile.FileName);
                string filePath = Path.Combine(coversFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await coverFile.CopyToAsync(fileStream);
                }

                // Delete old custom cover if it exists
                if (!string.IsNullOrEmpty(playlist.CoverPath) && playlist.CoverPath.StartsWith("/covers/"))
                {
                    string oldFilePath = Path.Combine(webRootPath, playlist.CoverPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch { /* ignore */ }
                    }
                }

                playlist.CoverPath = "/covers/" + uniqueFileName;
            }

            _context.Playlists.Update(playlist);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> AddSong(int playlistId, int songId, string? returnUrl = null)
        {
            // Verificăm dacă melodia este deja în playlist ca să nu o duplicăm
            bool alreadyExists = await _context.PlaylistSongs
                .AnyAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            if (!alreadyExists)
            {
                // Găsim ordinea piesei (TrackOrder) - o punem la final
                int trackOrder = await _context.PlaylistSongs
                    .Where(ps => ps.PlaylistId == playlistId)
                    .CountAsync() + 1;

                var playlistSong = new PlaylistSong
                {
                    PlaylistId = playlistId,
                    SongId = songId,
                    TrackOrder = trackOrder
                };

                _context.PlaylistSongs.Add(playlistSong);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Te întoarce fix înapoi în Librărie ca să îți continui treaba
            return RedirectToAction("Index", "Library");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string sortBy = "recent")
        {
            int currentUserId = GetCurrentUserId();

            var playlist = await _context.Playlists
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == currentUserId || p.IsShared));

            if (playlist == null)
            {
                return NotFound("Playlist not found or you don't have access to it.");
            }

            // Load and sort songs in the playlist
            var songsQuery = _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == id)
                .Include(ps => ps.Song);

            IOrderedQueryable<PlaylistSong> sortedSongsQuery;
            if (sortBy == "alpha")
            {
                sortedSongsQuery = songsQuery.OrderBy(ps => ps.Song.Title);
            }
            else
            {
                sortedSongsQuery = songsQuery.OrderByDescending(ps => ps.TrackOrder);
            }

            var playlistSongs = await sortedSongsQuery.ToListAsync();
            ViewBag.PlaylistSongs = playlistSongs;
            ViewBag.SortBy = sortBy;
            ViewBag.IsOwner = (playlist.UserId == currentUserId);
            
            // Check if this playlist is saved in the user's library
            ViewBag.IsSaved = await _context.SavedPlaylists
                .AnyAsync(sp => sp.UserId == currentUserId && sp.PlaylistId == id);

            // Fetch user songs so the user can easily add more songs to this playlist from its details page
            var userSongs = await _context.UserLibraries
                .Where(ul => ul.UserId == currentUserId)
                .Include(ul => ul.Song)
                .Select(ul => ul.Song)
                .ToListAsync();
            ViewBag.UserSongs = userSongs;

            return View(playlist);
        }

        // POST: /Playlist/RemoveSong
        [HttpPost]
        public async Task<IActionResult> RemoveSong(int playlistId, int songId)
        {
            int currentUserId = GetCurrentUserId();

            // Check if user owns the playlist
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == currentUserId);

            if (playlist == null)
            {
                return Forbid();
            }

            var playlistSong = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            if (playlistSong != null)
            {
                _context.PlaylistSongs.Remove(playlistSong);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = playlistId });
        }

        // POST: /Playlist/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            int currentUserId = GetCurrentUserId();

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

            if (playlist == null)
            {
                return Forbid();
            }

            // Remove associated songs from playlist
            var playlistSongs = await _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == id)
                .ToListAsync();

            if (playlistSongs.Any())
            {
                _context.PlaylistSongs.RemoveRange(playlistSongs);
            }

            // Remove playlist
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // POST: Save a public playlist to library
        [HttpPost]
        public async Task<IActionResult> Save(int playlistId)
        {
            int currentUserId = GetCurrentUserId();
            
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.IsShared && p.UserId != currentUserId);
                
            if (playlist == null)
            {
                return NotFound("Playlist not found or cannot be saved.");
            }
            
            var alreadySaved = await _context.SavedPlaylists
                .AnyAsync(sp => sp.UserId == currentUserId && sp.PlaylistId == playlistId);
                
            if (!alreadySaved)
            {
                var savedPlaylist = new SavedPlaylists
                {
                    UserId = currentUserId,
                    PlaylistId = playlistId
                };
                _context.SavedPlaylists.Add(savedPlaylist);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("Details", new { id = playlistId });
        }

        // POST: Unsave a public playlist
        [HttpPost]
        public async Task<IActionResult> Unsave(int playlistId)
        {
            int currentUserId = GetCurrentUserId();
            
            var savedPlaylist = await _context.SavedPlaylists
                .FirstOrDefaultAsync(sp => sp.UserId == currentUserId && sp.PlaylistId == playlistId);
                
            if (savedPlaylist != null)
            {
                _context.SavedPlaylists.Remove(savedPlaylist);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("Index");
        }

        // POST: Copy/Clone a public playlist to user's own playlists
        [HttpPost]
        public async Task<IActionResult> Clone(int playlistId)
        {
            int currentUserId = GetCurrentUserId();
            
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                .FirstOrDefaultAsync(p => p.Id == playlistId && (p.IsShared || p.UserId == currentUserId));
                
            if (playlist == null)
            {
                return NotFound("Playlist not found.");
            }
            
            var newPlaylist = new Playlist
            {
                Name = $"{playlist.Name} (Copy)",
                UserId = currentUserId,
                IsShared = false,
                CoverPath = playlist.CoverPath
            };
            
            _context.Playlists.Add(newPlaylist);
            await _context.SaveChangesAsync();
            
            if (playlist.PlaylistSongs != null && playlist.PlaylistSongs.Any())
            {
                var order = 1;
                foreach (var ps in playlist.PlaylistSongs)
                {
                    var newPlaylistSong = new PlaylistSong
                    {
                        PlaylistId = newPlaylist.Id,
                        SongId = ps.SongId,
                        TrackOrder = order++
                    };
                    _context.PlaylistSongs.Add(newPlaylistSong);
                }
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("Details", new { id = newPlaylist.Id });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(claim) ? 0 : int.Parse(claim);
        }
    }
}
