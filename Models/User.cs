using System.ComponentModel.DataAnnotations;

namespace Offstream.web.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string PasswordHash { get; set; }

        // Relații
        public virtual ICollection<UserLibrary>? UserLibraries { get; set; }
        public virtual ICollection<Playlist>? Playlists { get; set; }
    }
}
