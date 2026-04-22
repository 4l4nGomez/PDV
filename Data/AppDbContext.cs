using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.IO;

namespace BakeryPOS.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<ProductionLog> ProductionLogs { get; set; }
        public DbSet<CashMovement> CashMovements { get; set; }
        public DbSet<Shrinkage> Shrinkages { get; set; }
        public DbSet<DailyInventoryAudit> DailyInventoryAudits { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<Audit> Audits { get; set; }

        public AppDbContext()
        {
            try
            {
                // Ensure WAL mode for better concurrency in SQLite. This runs per-context creation but is idempotent.
                Database.OpenConnection();
                Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

                // Create audit table if it doesn't exist so audits won't fail on DBs without migrations
                EnsureAuditTableExists();
            }
            catch (Exception ex)
            {
                // Logging is safe and non-throwing by design
                Logger.Log("Error initializing DB pragmas/ensures", ex);
            }
            finally
            {
                try { Database.CloseConnection(); } catch { }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use a centralized settings helper for paths so behavior can be changed without editing the DbContext.
            var dbPath = Settings.DatabasePath;
            optionsBuilder.UseSqlite($"Data Source={dbPath};Cache=Shared");
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relación opcional para la auditoría de inventario
            modelBuilder.Entity<DailyInventoryAudit>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DailyInventoryAudit>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Seed initial admin and cajero user with BCrypt hashed passwords
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Role = "admin", PasswordHash = "$2a$11$75IpD3/Q0pbp6VhE39JrcupgqjR/pkDW/mS7G1Azp1ydJbEc1on0G" },
                new User { Id = 2, Username = "cajero", Role = "cajero", PasswordHash = "$2a$11$UMdwxpbiWwE.fOazIWs4weZeVNIOY1XCP6kgUSkS8ytUSgJjyiZpK" }
            );
        }

        private void EnsureAuditTableExists()
        {
            try
            {
                // SQLite is permissive with types; this creates a minimal table that EF can insert into.
                var sql = @"CREATE TABLE IF NOT EXISTS Audits (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UserId INTEGER,
                                Action TEXT,
                                Entity TEXT,
                                Data TEXT,
                                Timestamp TEXT,
                                ShiftId INTEGER
                             );";
                Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to ensure Audit table exists", ex);
            }
        }
    }
}
