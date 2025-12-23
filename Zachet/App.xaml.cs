using Microsoft.EntityFrameworkCore;
using Zachet.Data;
using Zachet.Models;
using System.Windows;


namespace Zachet
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new PractikDbContext();
            db.Database.EnsureCreated();

            if (!db.Warehouses.Any())
            {
                var wh = new Warehouse { Name = "Основной склад", Address = "г. Москва, ул. Складская, 1", IsActive = true };
                db.Warehouses.Add(wh);
                db.SaveChanges();

                db.StorageLocations.AddRange(
                    new StorageLocation { WarehouseId = wh.WarehouseId, Code = "A-01-01", MaxWeightKg = 500, MaxVolumeM3 = 5, IsAvailable = true },
                    new StorageLocation { WarehouseId = wh.WarehouseId, Code = "A-01-02", MaxWeightKg = 500, MaxVolumeM3 = 5, IsAvailable = true },
                    new StorageLocation { WarehouseId = wh.WarehouseId, Code = "B-02-01", MaxWeightKg = 1000, MaxVolumeM3 = 10, IsAvailable = true }
                );
                db.SaveChanges();
            }

            if (!db.Products.Any())
            {
                db.Products.Add(new Product
                {
                    SKU = "PRD-001",
                    Name = "Товар A",
                    WeightKg = 2,
                    VolumeM3 = 0.02m,
                    ShelfLifeDays = 365,
                    StorageConditions = "Сухое место"
                });
                db.SaveChanges();
            }
        }
    }
}