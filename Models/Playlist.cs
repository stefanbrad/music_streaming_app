using System.ComponentModel.DataAnnotations;

namespace Offstream.web.Models
{
    public class Playlist
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public bool IsShared { get; set; } = false;

        public int UserId { get; set; }
        public virtual User User { get; set; }
        public string? CoverPath { get; set; }

        public virtual ICollection<PlaylistSong>? PlaylistSongs
        {
            get; set;
        }
    }
}
