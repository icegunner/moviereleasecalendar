using Microsoft.EntityFrameworkCore;
using MovieReleaseCalendar.API.Models;

namespace MovieReleaseCalendar.API.Data
{
    public class MovieDbContext : DbContext
    {
        public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }

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
                entity.Property(e => e.TmdbId);
                entity.Property(e => e.ImdbId).HasMaxLength(20);
                entity.Property(e => e.MpaaRating).HasMaxLength(10);

                // Store genres as a comma-separated string
                entity.Property(e => e.Genres)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => new System.Collections.Generic.List<string>(v.Split(',', System.StringSplitOptions.RemoveEmptyEntries)))
                    .HasColumnType("text");

                // Store directors as a comma-separated string
                entity.Property(e => e.Directors)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => new System.Collections.Generic.List<string>(v.Split(',', System.StringSplitOptions.RemoveEmptyEntries)))
                    .HasColumnType("text");

                // Store cast as a comma-separated string
                entity.Property(e => e.Cast)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => new System.Collections.Generic.List<string>(v.Split(',', System.StringSplitOptions.RemoveEmptyEntries)))
                    .HasColumnType("text");

                entity.HasIndex(e => e.ReleaseDate);
                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => e.ImdbId);
                entity.HasIndex(e => e.MpaaRating);
            });

            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.ToTable("UserPreferences");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Theme).HasMaxLength(10);
                entity.Property(e => e.DefaultView).HasMaxLength(10);
                entity.Property(e => e.TmdbApiKey).HasMaxLength(500);
                entity.Property(e => e.CronSchedule).HasMaxLength(100);
                entity.Property(e => e.ShowRatings);
                entity.Property(e => e.EnableSwagger);
                entity.Property(e => e.UpdatedAt);
            });
        }
    }
}
