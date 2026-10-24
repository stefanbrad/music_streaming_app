using Microsoft.EntityFrameworkCore;
using Offstream.web.Models;

namespace Offstream.web.Data
{
    public class OffstreamDbContext : DbContext
    {
        public OffstreamDbContext(DbContextOptions<OffstreamDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<UserLibrary> UserLibraries { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public DbSet<SavedPlaylists> SavedPlaylists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Setăm cheile primare compuse pentru tabelele de legătură
            modelBuilder.Entity<UserLibrary>()
                .HasKey(ul => new { ul.UserId, ul.SongId });

            modelBuilder.Entity<PlaylistSong>()
                .HasKey(ps => new { ps.PlaylistId, ps.SongId });
        }
    }
}
