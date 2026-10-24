using System.ComponentModel.DataAnnotations;

namespace Offstream.web.Models
{
    public class Song
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Artist { get; set; }
        [Required]
        public string FileHash { get; set; } // in caz de deduplicare
        [Required]
        public string FilePath { get; set; }

        // Relationships
        public virtual ICollection<UserLibrary>? UserLibraries { get; set; }
        public virtual ICollection<PlaylistSong>? PlaylistSongs { get; set; }
    }
}
