using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class WarehousesPage : Page
    {
        public WarehousesPage()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                try
                {
                    await LoadWarehousesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки складов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private async Task LoadWarehousesAsync()
        {
            using var db = new PractikDbContext();
            var warehouses = await db.Warehouses.ToListAsync();
            WarehousesGrid.ItemsSource = warehouses;
        }

        private void AddWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window
            {
                Title = "Новый склад",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var nameBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var addrBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var saveBtn = new Button { Content = "Сохранить", Width = 100, Height = 30, Margin = new Thickness(0, 10, 0, 0) };

            var layout = new StackPanel { Margin = new Thickness(20) };
            layout.Children.Add(new TextBlock { Text = "Название*" });
            layout.Children.Add(nameBox);
            layout.Children.Add(new TextBlock { Text = "Адрес*" });
            layout.Children.Add(addrBox);
            layout.Children.Add(saveBtn);

            win.Content = layout;

            saveBtn.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(addrBox.Text))
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var db = new PractikDbContext();
                db.Warehouses.Add(new Warehouse
                {
                    Name = nameBox.Text.Trim(),
                    Address = addrBox.Text.Trim(),
                    IsActive = true
                });
                await db.SaveChangesAsync();
                await LoadWarehousesAsync();
                win.Close();
                MessageBox.Show("Склад успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            win.ShowDialog();
        }

        private async void DeleteWarehouse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int warehouseId)
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот склад?\nНа складе не должно быть ячеек хранения.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new PractikDbContext();

                // Проверка: есть ли ячейки на складе?
                bool hasLocations = await db.StorageLocations.AnyAsync(sl => sl.WarehouseId == warehouseId);
                if (hasLocations)
                {
                    MessageBox.Show("Нельзя удалить склад: на нём есть ячейки хранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var warehouse = await db.Warehouses.FindAsync(warehouseId);
                if (warehouse == null)
                {
                    MessageBox.Show("Склад не найден.");
                    return;
                }

                db.Warehouses.Remove(warehouse);
                await db.SaveChangesAsync();

                // Логирование
                db.ActionLogs.Add(new ActionLog
                {
                    ActionType = "Delete",
                    Entity = "Warehouse",
                    EntityId = warehouseId,
                    Comment = $"Удалён склад: {warehouse.Name}"
                });
                await db.SaveChangesAsync();

                await LoadWarehousesAsync();
                MessageBox.Show("Склад успешно удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении склада: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}