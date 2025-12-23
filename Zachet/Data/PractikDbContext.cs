using Microsoft.EntityFrameworkCore;
using Zachet.Models;

namespace Zachet.Data
{
    public class PractikDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PractikDb;Integrated Security=true;");
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Movement> Movements => Set<Movement>();
        public DbSet<InventoryCheck> InventoryChecks => Set<InventoryCheck>();
        public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StorageLocation>()
                .HasOne(sl => sl.Warehouse)
                .WithMany()
                .HasForeignKey(sl => sl.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Location)
                .WithMany()
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany()
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movement>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movement>()
                .HasOne(m => m.ToLocation)
                .WithMany()
                .HasForeignKey(m => m.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryCheck>()
                .HasOne(ic => ic.Location)
                .WithMany()
                .HasForeignKey(ic => ic.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryCheck>()
                .HasOne(ic => ic.Product)
                .WithMany()
                .HasForeignKey(ic => ic.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public StorageLocation? FindBestLocationFor(Product product, int quantity)
        {
            var totalWeight = product.WeightKg * quantity;
            var totalVolume = product.VolumeM3 * quantity;

            return StorageLocations
                .Include(sl => sl.Warehouse)
                .Where(sl => sl.IsAvailable)
                .Where(sl => sl.MaxWeightKg >= totalWeight)
                .Where(sl => sl.MaxVolumeM3 >= totalVolume)
                .Where(sl =>
                    string.IsNullOrEmpty(product.StorageConditions) ||
                    (product.StorageConditions.Contains("Холод") && sl.Warehouse.Name.Contains("Холод")) ||
                    (product.StorageConditions.Contains("Температур") && sl.Warehouse.Name.Contains("Температур"))
                )
                .OrderBy(sl => sl.Warehouse.Name)
                .ThenBy(sl => sl.Code)
                .FirstOrDefault();
        }
    }
}