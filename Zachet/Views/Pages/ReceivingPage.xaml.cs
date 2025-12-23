using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class ReceivingPage : Page
    {
        public ReceivingPage()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            using var db = new PractikDbContext();
            ProductCombo.ItemsSource = await db.Products.ToListAsync();
            LocationCombo.ItemsSource = await db.StorageLocations.Include(l => l.Warehouse).ToListAsync();
        }

        private async void Receive_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (ProductCombo.SelectedItem == null || LocationCombo.SelectedItem == null)
            {
                ErrorText.Text = "Выберите товар и ячейку.";
                return;
            }

            var product = (Product)ProductCombo.SelectedItem;
            var location = (StorageLocation)LocationCombo.SelectedItem;

            if (!int.TryParse(QtyBox.Text, out var qty) || qty <= 0)
            {
                ErrorText.Text = "Некорректное количество.";
                return;
            }

            DateTime? expiry = null;
            if (!string.IsNullOrWhiteSpace(ExpiryBox.Text))
            {
                if (!DateTime.TryParseExact(ExpiryBox.Text, "dd.MM.yyyy", null, DateTimeStyles.None, out var d))
                {
                    ErrorText.Text = "Неверный формат срока годности. Используйте дд.мм.гггг";
                    return;
                }
                expiry = d;
            }

            using var db = new PractikDbContext();

            var currentWeight = (await db.Inventories
                .Where(i => i.LocationId == location.LocationId)
                .SumAsync(i => (long?)i.Quantity * (long?)i.Product.WeightKg)) ?? 0m;
            var currentVolume = (await db.Inventories
                .Where(i => i.LocationId == location.LocationId)
                .SumAsync(i => (long?)i.Quantity * (long?)i.Product.VolumeM3)) ?? 0m;

            var newWeight = currentWeight + qty * product.WeightKg;
            var newVolume = currentVolume + qty * product.VolumeM3;

            if (newWeight > location.MaxWeightKg || newVolume > location.MaxVolumeM3)
            {
                ErrorText.Text = "Превышена грузоподъёмность или объём ячейки.";
                return;
            }

            var inv = await db.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == product.ProductId && i.LocationId == location.LocationId && i.BatchNumber == BatchBox.Text && i.ExpiryDate == expiry);

            if (inv == null)
            {
                inv = new Inventory
                {
                    ProductId = product.ProductId,
                    LocationId = location.LocationId,
                    Quantity = 0,
                    Reserved = 0,
                    BatchNumber = string.IsNullOrWhiteSpace(BatchBox.Text) ? null : BatchBox.Text,
                    ExpiryDate = expiry
                };
                db.Inventories.Add(inv);
            }

            inv.Quantity += qty;
            await db.SaveChangesAsync();

            db.Movements.Add(new Movement
            {
                ProductId = product.ProductId,
                FromLocationId = null,
                ToLocationId = location.LocationId,
                Quantity = qty,
                Reason = "Приход"
            });

            db.ActionLogs.Add(new ActionLog
            {
                ActionType = "Receiving",
                Entity = "Inventory",
                EntityId = inv.InventoryId,
                ProductId = product.ProductId,
                ToLocationId = location.LocationId,
                Quantity = qty,
                Comment = $"Принято {qty} ед. товара {product.Name} в ячейку {location.Code}"
            });

            await db.SaveChangesAsync();
            MessageBox.Show("Товар успешно принят!");
            NavigationService.GoBack();
        }
    }
}