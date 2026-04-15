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

        public AppDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BakeryPOS");
            
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string dbPath = Path.Combine(folder, "bakery_pos.db");
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
    }
}
