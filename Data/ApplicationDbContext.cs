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
        public DbSet<ScanResult> ScanResults { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<DepartmentAccess> DepartmentAccesses { get; set; }

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
                .Property(t => t.CreatedBy)
                .HasMaxLength(100);

            modelBuilder.Entity<Template>()
                .Property(t => t.CreatedByID)
                .HasMaxLength(50);

            modelBuilder.Entity<Template>()
                .Property(t => t.UpdatedBy)
                .HasMaxLength(100);

            modelBuilder.Entity<Template>()
                .Property(t => t.UpdatedByID)
                .HasMaxLength(50);

            modelBuilder.Entity<Template>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Template>()
                .Property(t => t.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Scan setting entity configuration 
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

            // ScanResult configuration
            modelBuilder.Entity<ScanResult>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<ScanResult>()
                .HasOne(s => s.Card)
                .WithMany()
                .HasForeignKey(s => s.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserProfile configuration
            modelBuilder.Entity<UserProfile>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Detail_TH_FirstName)
                .HasMaxLength(100);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Detail_TH_LastName)
                .HasMaxLength(100);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Detail_EN_FirstName)
                .HasMaxLength(100);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Detail_EN_LastName)
                .HasMaxLength(100);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.User_Email)
                .HasMaxLength(255);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.User_Code)
                .HasMaxLength(50);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Department_Name)
                .HasMaxLength(100);

            modelBuilder.Entity<UserProfile>()
                .Property(u => u.Plant_Name)
                .HasMaxLength(100);

            // Configure Role entity
            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Role>()
                .Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Role>()
                .Property(r => r.NormalizedName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.NormalizedName)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .Property(r => r.Description)
                .HasMaxLength(255);

            modelBuilder.Entity<Role>()
                .Property(r => r.ConcurrencyStamp)
                .IsRequired();

            // Configure UserRole entity
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => ur.Id);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create a unique index on UserId and RoleId to prevent duplicate role assignments
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .Property(ur => ur.CreatedBy)
                .HasMaxLength(100);

            // MaintenanceReminder configuration
            modelBuilder.Entity<MaintenanceReminder>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<MaintenanceReminder>()
                .HasOne(m => m.Card)
                .WithMany()
                .HasForeignKey(m => m.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaintenanceReminder>()
                .Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<MaintenanceReminder>()
                .Property(m => m.Notes)
                .IsRequired(false);

            modelBuilder.Entity<MaintenanceReminder>()
                .Property(m => m.RepeatFrequency)
                .HasMaxLength(20)
                .HasDefaultValue("never");

            modelBuilder.Entity<MaintenanceReminder>()
                .Property(m => m.CreatedBy)
                .HasMaxLength(100);

            // CardDocument configuration
            modelBuilder.Entity<CardDocument>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<CardDocument>()
                .HasOne(d => d.Card)
                .WithMany()
                .HasForeignKey(d => d.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.DocumentType)
                .HasMaxLength(50);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.Description)
                .IsRequired(false);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.FileType)
                .HasMaxLength(100);

            modelBuilder.Entity<CardDocument>()
                .Property(d => d.UploadedBy)
                .HasMaxLength(100);
            // Configure DepartmentAccess entity
            modelBuilder.Entity<DepartmentAccess>()
                .HasKey(da => da.Id);

            modelBuilder.Entity<DepartmentAccess>()
                .HasOne(da => da.User)
                .WithMany()
                .HasForeignKey(da => da.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DepartmentAccess>()
                .Property(da => da.DepartmentName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<DepartmentAccess>()
                .Property(da => da.AccessLevel)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("View");

            modelBuilder.Entity<DepartmentAccess>()
                .Property(da => da.GrantedBy)
                .HasMaxLength(100);

            modelBuilder.Entity<DepartmentAccess>()
                .Property(da => da.GrantedById)
                .HasMaxLength(50);

            // Seed default roles with static values
            modelBuilder.Entity<Role>().HasData(
                // Existing roles remain unchanged (1-3)
                new Role
                {
                    Id = 1,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Administrator with full system access",
                    CreatedAt = new DateTime(2023, 1, 1),
                    ConcurrencyStamp = "8f9460e9-0637-4c21-8002-3048e67fc674"
                },
                new Role
                {
                    Id = 2,
                    Name = "Manager",
                    NormalizedName = "MANAGER",
                    Description = "Manager with limited administrative access, Department Access menu and Scan results",
                    CreatedAt = new DateTime(2023, 1, 1),
                    ConcurrencyStamp = "c2d6d1e4-4f50-40de-9d12-849722d39b27"
                },
                new Role
                {
                    Id = 3,
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Standard user with departmental access, can't access Department Access menu",
                    CreatedAt = new DateTime(2023, 1, 1),
                    ConcurrencyStamp = "3f36708b-6b0e-4630-b31b-51d0d8f3956d"
                },
                // Add new roles
                new Role
                {
                    Id = 4,
                    Name = "Edit",
                    NormalizedName = "EDIT",
                    Description = "Can access Scan results and edit departmental data with authorized access",
                    CreatedAt = new DateTime(2023, 1, 1),
                    ConcurrencyStamp = "5a92f40b-7c2e-4ec9-a637-d7d3c8e9f8a0"
                },
                new Role
                {
                    Id = 5,
                    Name = "View",
                    NormalizedName = "VIEW",
                    Description = "Limited access to view QR codes only",
                    CreatedAt = new DateTime(2023, 1, 1),
                    ConcurrencyStamp = "7b81d6c1-3e7f-48d3-9c2b-1b7ba4582c9d"
                }
            );

        }
    }
}