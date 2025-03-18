// Path: Data/ApplicationDbContext.cs
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
        public DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }
        public DbSet<CardDocument> CardDocuments { get; set; }

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
                
            // Configure MaintenanceReminder
            modelBuilder.Entity<MaintenanceReminder>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<MaintenanceReminder>()
                .HasOne(r => r.Card)
                .WithMany()
                .HasForeignKey(r => r.CardId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<MaintenanceReminder>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            modelBuilder.Entity<MaintenanceReminder>()
                .Property(r => r.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            // Configure CardDocument
            modelBuilder.Entity<CardDocument>()
                .HasKey(d => d.Id);
                
            modelBuilder.Entity<CardDocument>()
                .HasOne(d => d.Card)
                .WithMany()
                .HasForeignKey(d => d.CardId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<CardDocument>()
                .Property(d => d.UploadedAt)
                .HasDefaultValueSql("GETDATE()");
        }
    }
}