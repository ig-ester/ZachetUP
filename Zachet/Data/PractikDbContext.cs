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
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Количество должно быть положительным.");

            var totalWeight = product.WeightKg * quantity;
            var totalVolume = product.VolumeM3 * quantity;

            // Поиск всех подходящих ячеек
            var candidateLocations = StorageLocations
                .Include(sl => sl.Warehouse)
                .Where(sl => sl.IsAvailable)
                .Where(sl => sl.MaxWeightKg >= totalWeight)
                .Where(sl => sl.MaxVolumeM3 >= totalVolume);

            // Фильтрация по условиям хранения (если заданы)
            if (!string.IsNullOrEmpty(product.StorageConditions))
            {
                var conditions = product.StorageConditions;
                candidateLocations = candidateLocations.Where(sl =>
                    (conditions.Contains("Холод") && sl.Warehouse.Name.Contains("Холод")) ||
                    (conditions.Contains("Температур") && sl.Warehouse.Name.Contains("Температур")) ||
                    (!conditions.Contains("Холод") && !conditions.Contains("Температур"))
                );
            }

            // Сначала пробуем найти ячейки, где товар уже лежит
            var usedLocationIds = Inventories
                .Where(i => i.ProductId == product.ProductId)
                .Select(i => i.LocationId)
                .ToHashSet();

            var preferred = candidateLocations
                .Where(sl => usedLocationIds.Contains(sl.LocationId))
                .OrderBy(sl => sl.Warehouse.Name)
                .ThenBy(sl => sl.Code)
                .FirstOrDefault();

            if (preferred != null)
            {
                // Проверяем, влезет ли товар в уже используемую ячейку
                if (CanFitInLocation(preferred.LocationId, product, quantity))
                {
                    return preferred;
                }
            }

            // Если не подошла существующая — ищем любую свободную подходящую
            return candidateLocations
                .Where(sl => !usedLocationIds.Contains(sl.LocationId))
                .OrderBy(sl => sl.Warehouse.Name)
                .ThenBy(sl => sl.Code)
                .FirstOrDefault();
        }

        /// <summary>
        /// Проверяет, влезет ли заданное количество товара в указанную ячейку с учётом текущих остатков.
        /// </summary>
        private bool CanFitInLocation(int locationId, Product product, int quantity)
        {
            var currentWeight = Inventories
                .Where(i => i.LocationId == locationId)
                .Sum(i => (decimal?)(i.Quantity * i.Product.WeightKg)) ?? 0m;

            var currentVolume = Inventories
                .Where(i => i.LocationId == locationId)
                .Sum(i => (decimal?)(i.Quantity * i.Product.VolumeM3)) ?? 0m;

            var newWeight = currentWeight + product.WeightKg * quantity;
            var newVolume = currentVolume + product.VolumeM3 * quantity;

            var location = StorageLocations.Find(locationId);
            if (location == null) return false;

            return newWeight <= location.MaxWeightKg && newVolume <= location.MaxVolumeM3;
        }
    }
}