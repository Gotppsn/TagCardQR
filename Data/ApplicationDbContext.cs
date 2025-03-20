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
                .IsRequired(false) // Make this optional since we set a default
                .HasMaxLength(100);
                
            // Make these properties optional
            modelBuilder.Entity<Card>()
                .Property(c => c.ImagePath)
                .IsRequired(false);
                
            modelBuilder.Entity<Card>()
                .Property(c => c.CustomFieldsData)
                .IsRequired(false)
                .HasDefaultValue("{}");

            // Set database-generated properties for timestamps
            modelBuilder.Entity<Card>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Card>()
                .Property(c => c.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Configure CardHistory
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
                
            // Configure IssueReport
            modelBuilder.Entity<IssueReport>()
                .HasKey(ir => ir.Id);
                
            modelBuilder.Entity<IssueReport>()
                .HasOne(ir => ir.Card)
                .WithMany()
                .HasForeignKey(ir => ir.CardId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<IssueReport>()
                .Property(ir => ir.CreatedAt)
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
        }
    }
}