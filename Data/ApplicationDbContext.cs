using CardTagManager.Models;
using Microsoft.EntityFrameworkCore;

namespace CardTagManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Card> Cards { get; set; }
        public DbSet<CardHistory> CardHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Card entity
            modelBuilder.Entity<Card>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Card>()
                .Property(c => c.ProductName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Card>()
                .Property(c => c.Manufacturer)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Card>()
                .Property(c => c.Category)
                .IsRequired()
                .HasMaxLength(100);
                
            // Make ImagePath optional (nullable)
            modelBuilder.Entity<Card>()
                .Property(c => c.ImagePath)
                .IsRequired(false);

            // Set up database-generated properties for CreatedAt and UpdatedAt
            modelBuilder.Entity<Card>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Card>()
                .Property(c => c.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CardHistory>()
                .HasKey(ch => ch.Id);

            modelBuilder.Entity<CardHistory>()
                .HasOne(ch => ch.Card)
                .WithMany()
                .HasForeignKey(ch => ch.CardId);                
        }
    }
}