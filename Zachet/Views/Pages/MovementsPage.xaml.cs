using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class MovementsPage : Page
    {
        public MovementsPage()
        {
            InitializeComponent();
            LoadLocations();
        }

        private async void LoadLocations()
        {
            using var db = new PractikDbContext();
            var locations = await db.StorageLocations.Include(l => l.Warehouse).ToListAsync();
            FromLocationCombo.ItemsSource = locations;
            ToLocationCombo.ItemsSource = locations;
        }

        private async void FromLocationChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FromLocationCombo.SelectedItem is not StorageLocation loc) return;

            using var db = new PractikDbContext();
            var inventories = await db.Inventories
                .Include(i => i.Product)
                .Where(i => i.LocationId == loc.LocationId && i.Quantity > 0)
                .ToListAsync();

            ProductCombo.ItemsSource = inventories;
            InfoText.Text = $"Доступно товаров: {inventories.Count}";
        }

        private async void Move_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (FromLocationCombo.SelectedItem == null || ToLocationCombo.SelectedItem == null || ProductCombo.SelectedItem == null)
            {
                ErrorText.Text = "Заполните все поля.";
                return;
            }

            var fromLoc = (StorageLocation)FromLocationCombo.SelectedItem;
            var toLoc = (StorageLocation)ToLocationCombo.SelectedItem;
            var inv = (Inventory)ProductCombo.SelectedItem;

            if (fromLoc.LocationId == toLoc.LocationId)
            {
                ErrorText.Text = "Исходная и целевая ячейки должны отличаться.";
                return;
            }

            if (!int.TryParse(QtyBox.Text, out var qty) || qty <= 0)
            {
                ErrorText.Text = "Некорректное количество.";
                return;
            }

            if (qty > inv.Quantity)
            {
                ErrorText.Text = "Недостаточно товара в исходной ячейке.";
                return;
            }

            using var db = new PractikDbContext();

            var product = await db.Products.FindAsync(inv.ProductId);
            var currentWeight = (await db.Inventories
                .Where(i => i.LocationId == toLoc.LocationId)
                .SumAsync(i => (long?)i.Quantity * (long?)i.Product.WeightKg)) ?? 0m;
            var currentVolume = (await db.Inventories
                .Where(i => i.LocationId == toLoc.LocationId)
                .SumAsync(i => (long?)i.Quantity * (long?)i.Product.VolumeM3)) ?? 0m;

            var newWeight = currentWeight + qty * product.WeightKg;
            var newVolume = currentVolume + qty * product.VolumeM3;

            if (newWeight > toLoc.MaxWeightKg || newVolume > toLoc.MaxVolumeM3)
            {
                ErrorText.Text = "Целевая ячейка не вмещает товар по весу или объёму.";
                return;
            }

            inv.Quantity -= qty;
            if (inv.Quantity == 0)
            {
                db.Inventories.Remove(inv);
            }

            var targetInv = await db.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == inv.ProductId && i.LocationId == toLoc.LocationId && i.BatchNumber == inv.BatchNumber && i.ExpiryDate == inv.ExpiryDate);

            if (targetInv == null)
            {
                targetInv = new Inventory
                {
                    ProductId = inv.ProductId,
                    LocationId = toLoc.LocationId,
                    Quantity = 0,
                    Reserved = 0,
                    BatchNumber = inv.BatchNumber,
                    ExpiryDate = inv.ExpiryDate
                };
                db.Inventories.Add(targetInv);
            }
            targetInv.Quantity += qty;

            db.Movements.Add(new Movement
            {
                ProductId = inv.ProductId,
                FromLocationId = fromLoc.LocationId,
                ToLocationId = toLoc.LocationId,
                Quantity = qty,
                Reason = ReasonBox.Text
            });

            db.ActionLogs.Add(new ActionLog
            {
                ActionType = "Movement",
                Entity = "Movement",
                EntityId = -1,
                ProductId = inv.ProductId,
                FromLocationId = fromLoc.LocationId,
                ToLocationId = toLoc.LocationId,
                Quantity = qty,
                Comment = $"Перемещено {qty} ед. из {fromLoc.Code} в {toLoc.Code}"
            });

            await db.SaveChangesAsync();
            MessageBox.Show("Перемещение выполнено!");
            NavigationService.GoBack();
        }
    }
}