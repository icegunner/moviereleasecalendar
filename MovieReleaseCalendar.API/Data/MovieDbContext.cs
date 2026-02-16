using Microsoft.EntityFrameworkCore;
using MovieReleaseCalendar.API.Models;

namespace MovieReleaseCalendar.API.Data
{
    public class MovieDbContext : DbContext
    {
        public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Movie>(entity =>
            {
                entity.ToTable("Movies");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(255);
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Url).HasMaxLength(1000);
                entity.Property(e => e.PosterUrl).HasMaxLength(1000);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.ReleaseDate);
                entity.Property(e => e.ScrapedAt);

                // Store genres as a comma-separated string
                entity.Property(e => e.Genres)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => new System.Collections.Generic.List<string>(v.Split(',', System.StringSplitOptions.RemoveEmptyEntries)))
                    .HasColumnType("text");

                entity.HasIndex(e => e.ReleaseDate);
            });
        }
    }
}
