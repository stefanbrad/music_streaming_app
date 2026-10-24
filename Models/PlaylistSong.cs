namespace Offstream.web.Models
{
    public class PlaylistSong
    {
        public int PlaylistId { get; set; }
        public virtual Playlist Playlist { get; set; }

        public int SongId { get; set; }
        public virtual Song Song { get; set; }

        public int TrackOrder { get; set; }
    }
}
