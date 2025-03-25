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
        public DbSet<IssueReport> IssueReports { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<ScanSettings> ScanSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Card entity
            modelBuilder.Entity<Card>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Card>()
                .Property(c => c.ProductName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Card>()
                .Property(c => c.Category)
                .IsRequired(false)
                .HasMaxLength(100);
                
            modelBuilder.Entity<Card>()
                .Property(c => c.ImagePath)
                .IsRequired(false);
                
            modelBuilder.Entity<Card>()
                .Property(c => c.CustomFieldsData)
                .IsRequired(false)
                .HasDefaultValue("{}");

            modelBuilder.Entity<Card>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Card>()
                .Property(c => c.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            // Configure CardHistory entity
            modelBuilder.Entity<CardHistory>()
                .HasKey(h => h.Id);
                
            modelBuilder.Entity<CardHistory>()
                .HasOne(h => h.Card)
                .WithMany()
                .HasForeignKey(h => h.CardId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<CardHistory>()
                .Property(h => h.FieldName)
                .IsRequired()
                .HasMaxLength(50);
                
            modelBuilder.Entity<CardHistory>()
                .Property(h => h.OldValue)
                .IsRequired(false)
                .HasMaxLength(500);
                
            modelBuilder.Entity<CardHistory>()
                .Property(h => h.NewValue)
                .IsRequired(false)
                .HasMaxLength(500);
                
            modelBuilder.Entity<CardHistory>()
                .Property(h => h.ChangedAt)
                .HasDefaultValueSql("GETDATE()");

            // Template entity configuration 
            modelBuilder.Entity<Template>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<Template>()
                .Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Template>()
                .Property(t => t.Category)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Template>()
                .Property(t => t.Icon)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Template>()
                .Property(t => t.BgColor)
                .IsRequired()
                .HasMaxLength(20);

            modelBuilder.Entity<Template>()
                .Property(t => t.IconColor)
                .HasMaxLength(50);

            modelBuilder.Entity<Template>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Template>()
                .Property(t => t.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            //Scan setting entity configuration 
            modelBuilder.Entity<ScanSettings>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<ScanSettings>()
                .HasOne(s => s.Card)
                .WithMany()
                .HasForeignKey(s => s.CardId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // IssueReport configuration
            modelBuilder.Entity<IssueReport>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<IssueReport>()
                .HasOne(r => r.Card)
                .WithMany()
                .HasForeignKey(r => r.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IssueReport>()
                .Property(r => r.IssueType)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<IssueReport>()
                .Property(r => r.Priority)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Medium");

            modelBuilder.Entity<IssueReport>()
                .Property(r => r.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Open");

            modelBuilder.Entity<IssueReport>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
            
            modelBuilder.Entity<IssueReport>()
                .Property(r => r.ImagePath)
                .IsRequired(false)
                .HasMaxLength(500);
        }
    }
}