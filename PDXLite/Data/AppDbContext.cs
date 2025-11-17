using Microsoft.EntityFrameworkCore;
using PDXLite.Models;

namespace PDXLite.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ExtractionHistory> ExtractionHistories { get; set; }
        public DbSet<PageVisit> PageVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.ApiKey).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FullName).IsRequired();
                entity.Property(e => e.ApiKey).IsRequired();
            });

            modelBuilder.Entity<ExtractionHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.ExtractionHistories)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PageVisit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Page).IsRequired();
                entity.HasIndex(e => e.VisitedAt);
            });
        }
    }
}
