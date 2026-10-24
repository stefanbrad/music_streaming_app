using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Offstream.web.Models
{
    public class SavedPlaylists
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; } // Cine a dat save
        public virtual User User { get; set; }

        public int PlaylistId { get; set; } // Ce playlist a salvat (creat de altcineva)
        public virtual Playlist Playlist { get; set; }
    }
}
