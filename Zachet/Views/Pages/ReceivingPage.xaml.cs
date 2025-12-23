using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class ReceivingPage : Page
    {
        private List<ReceivingItem> _items = new();
        private List<StorageLocation> _allLocations = new();

        public List<StorageLocation> AllLocations => _allLocations;

        public ReceivingPage()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                try
                {
                    await LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                }
            };
        }

        private async Task LoadProducts()
        {
            using var db = new PractikDbContext();
            var products = await db.Products.ToListAsync();
            _allLocations = await db.StorageLocations
                .Include(sl => sl.Warehouse)
                .Where(sl => sl.IsAvailable)
                .ToListAsync();
            _items = products.Select(p => new ReceivingItem { Product = p }).ToList();
            ProductsGrid.ItemsSource = _items;
            ProductsGrid.DataContext = this;
        }

        private async void AcceptReceiving_Click(object sender, RoutedEventArgs e)
        {
            var selected = _items.Where(i => i.IsSelected && i.Quantity > 0).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Выберите хотя бы один товар с количеством > 0.");
                return;
            }

            using var db = new PractikDbContext();

            foreach (var item in selected)
            {
                var product = item.Product;
                var quantity = item.Quantity;
                var userLocation = item.SelectedLocation;

                StorageLocation? targetLocation = null;

                if (userLocation != null)
                {
                    if (await CanFit(db, userLocation.LocationId, product, quantity))
                    {
                        targetLocation = userLocation;
                    }
                    else
                    {
                        MessageBox.Show($"Ячейка {userLocation.Code} не вмещает {product.Name} по весу или объёму.");
                        return;
                    }
                }
                else
                {
                    var currentInventories = await db.Inventories
                        .Include(i => i.Location)
                        .Where(i => i.ProductId == product.ProductId)
                        .ToListAsync();

                    foreach (var inv in currentInventories)
                    {
                        if (await CanFit(db, inv.LocationId, product, quantity))
                        {
                            targetLocation = inv.Location;
                            break;
                        }
                    }

                    if (targetLocation == null)
                    {
                        targetLocation = db.FindBestLocationFor(product, quantity);
                        if (targetLocation == null)
                        {
                            MessageBox.Show($"Не найдено подходящей ячейки для товара: {product.Name}");
                            return;
                        }
                    }
                }

                var existing = await db.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == product.ProductId && i.LocationId == targetLocation.LocationId);

                if (existing != null)
                {
                    existing.Quantity += quantity;
                }
                else
                {
                    db.Inventories.Add(new Inventory
                    {
                        ProductId = product.ProductId,
                        LocationId = targetLocation.LocationId,
                        Quantity = quantity,
                        Reserved = 0,
                        ExpiryDate = product.ShelfLifeDays.HasValue
                            ? DateTime.UtcNow.AddDays(product.ShelfLifeDays.Value)
                            : (DateTime?)null,
                        BatchNumber = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    });
                }

                db.Movements.Add(new Movement
                {
                    ProductId = product.ProductId,
                    FromLocationId = null,
                    ToLocationId = targetLocation.LocationId,
                    Quantity = quantity,
                    Reason = "Приход"
                });

                db.ActionLogs.Add(new ActionLog
                {
                    ActionType = "Receiving",
                    Entity = "Inventory",
                    EntityId = existing?.InventoryId ?? -1,
                    ProductId = product.ProductId,
                    ToLocationId = targetLocation.LocationId,
                    Quantity = quantity,
                    Comment = $"Приход {product.Name}, кол-во: {quantity}, ячейка: {targetLocation.Code}"
                });
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Приёмка успешно завершена.");
            NavigationService.GoBack();
        }

        private static async Task<bool> CanFit(PractikDbContext db, int locationId, Product product, int quantity)
        {
            var currentWeight = await db.Inventories
                .Where(i => i.LocationId == locationId)
                .SumAsync(i => (decimal?)(i.Quantity * i.Product.WeightKg)) ?? 0m;

            var currentVolume = await db.Inventories
                .Where(i => i.LocationId == locationId)
                .SumAsync(i => (decimal?)(i.Quantity * i.Product.VolumeM3)) ?? 0m;

            var newWeight = currentWeight + quantity * (decimal)product.WeightKg;
            var newVolume = currentVolume + quantity * (decimal)product.VolumeM3;

            var location = await db.StorageLocations.FindAsync(locationId);
            if (location == null) return false;

            return newWeight <= location.MaxWeightKg && newVolume <= location.MaxVolumeM3;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}