namespace Offstream.web.Models
{
    public class UserLibrary
    {
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int SongId { get; set; }
        public virtual Song Song { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
