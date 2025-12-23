using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;
using Zachet.Views.Pages;

namespace Zachet.Views.Pages
{
    public partial class ReceivingPage : Page
    {
        private List<ReceivingItem> _items = new();

        public ReceivingPage()
        {
            InitializeComponent();
            LoadProducts();
        }

        private async void LoadProducts()
        {
            using var db = new PractikDbContext();
            var products = await db.Products.ToListAsync();
            _items = products.Select(p => new ReceivingItem { Product = p }).ToList();
            ProductsGrid.ItemsSource = _items;
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Product product)
            {
                NavigationService.Navigate(new ProductEditPage(product));
            }
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
                var location = db.FindBestLocationFor(item.Product, item.Quantity);
                if (location == null)
                {
                    MessageBox.Show($"Не найдено подходящей ячейки для товара: {item.Product.Name}");
                    return;
                }

                var existing = await db.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.Product.ProductId && inv.LocationId == location.LocationId);

                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    db.Inventories.Add(new Inventory
                    {
                        ProductId = item.Product.ProductId,
                        LocationId = location.LocationId,
                        Quantity = item.Quantity,
                        Reserved = 0,
                        ExpiryDate = item.Product.ShelfLifeDays.HasValue
                            ? DateTime.UtcNow.AddDays(item.Product.ShelfLifeDays.Value)
                            : (DateTime?)null,
                        BatchNumber = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    });
                }

                db.Movements.Add(new Movement
                {
                    ProductId = item.Product.ProductId,
                    FromLocationId = null,
                    ToLocationId = location.LocationId,
                    Quantity = item.Quantity,
                    PerformedBy = Environment.UserName,
                    Reason = "Приход"
                });

                db.ActionLogs.Add(new ActionLog
                {
                    ActionType = "Receiving",
                    Entity = "Inventory",
                    EntityId = existing?.InventoryId ?? -1,
                    ProductId = item.Product.ProductId,
                    ToLocationId = location.LocationId,
                    Quantity = item.Quantity,
                    Comment = $"Приход товара {item.Product.Name}, кол-во: {item.Quantity}"
                });
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Приёмка успешно завершена.");
            NavigationService.GoBack();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}