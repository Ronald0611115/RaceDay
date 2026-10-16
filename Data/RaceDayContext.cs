
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(
            DbContextOptions<RaceDayContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<EventRoute> EventRoutes { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Event");
                entity.HasKey(e => e.EventId);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");
                entity.HasKey(c => c.CategoryId);
                entity.Property(c => c.EntryFee)
                    .HasPrecision(10, 2);
            });

            modelBuilder.Entity<EventRoute>(entity =>
            {
                entity.ToTable("EventRoute");
                entity.HasKey(r => r.RouteId);
                entity.Property(r => r.DistanceKm)
                    .HasPrecision(5, 2);
                entity.HasIndex(r => r.CategoryId).IsUnique();
            });

            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.ToTable("Enrolment");
                entity.HasKey(e => e.EnrolmentId);
                entity.Property(e => e.EnrolmentDate)
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Status)
                    .HasDefaultValue("Confirmed");
            });

            modelBuilder.Entity<Result>(entity =>
            {
                entity.ToTable("Result");
                entity.HasKey(r => r.ResultId);
                entity.Property(r => r.RecordedAt)
                    .HasDefaultValueSql("GETDATE()");
                entity.HasIndex(r => r.EnrolmentId).IsUnique();
            });
        }
    }
}
