using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;
using Zachet.Views.Pages;

namespace Zachet.Views.Pages
{
    public partial class StorageLocationsPage : Page
    {
        public StorageLocationsPage()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                try
                {
                    await LoadAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки мест хранения: {ex.Message}");
                }
            };
        }

        private async Task LoadAsync()
        {
            using var db = new PractikDbContext();
            var locations = await db.StorageLocations
                .Include(l => l.Warehouse)
                .ToListAsync();
            Grid.ItemsSource = locations;
        }

        private async void DeleteLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int locId) return;

            var confirm = MessageBox.Show("Удалить ячейку? Она должна быть пустой.", "Подтверждение", MessageBoxButton.YesNo);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = new PractikDbContext();

                if (await db.Inventories.AnyAsync(i => i.LocationId == locId))
                {
                    MessageBox.Show("Нельзя удалить: в ячейке есть товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var loc = await db.StorageLocations.FindAsync(locId);
                if (loc == null) return;

                db.StorageLocations.Remove(loc);
                await db.SaveChangesAsync();

                db.ActionLogs.Add(new ActionLog
                {
                    ActionType = "Delete",
                    Entity = "StorageLocation",
                    EntityId = locId,
                    Comment = $"Удалена ячейка: {loc.Code}"
                });
                await db.SaveChangesAsync();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            List<Warehouse> warehouses;
            using (var db = new PractikDbContext())
            {
                warehouses = db.Warehouses.ToList();
            }

            if (!warehouses.Any())
            {
                MessageBox.Show("Сначала создайте склад.");
                return;
            }

            var win = new Window
            {
                Title = "Новое место хранения",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            var whBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            foreach (var wh in warehouses)
                whBox.Items.Add(new { Id = wh.WarehouseId, Name = wh.Name });
            whBox.DisplayMemberPath = "Name";
            whBox.SelectedIndex = 0;

            var codeBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var weightBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var volumeBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var saveBtn = new Button { Content = "Сохранить", Width = 100, Height = 30 };

            sp.Children.Add(new TextBlock { Text = "Склад*" });
            sp.Children.Add(whBox);
            sp.Children.Add(new TextBlock { Text = "Код (A-01-01)*" });
            sp.Children.Add(codeBox);
            sp.Children.Add(new TextBlock { Text = "Макс. вес (кг)*" });
            sp.Children.Add(weightBox);
            sp.Children.Add(new TextBlock { Text = "Макс. объём (м³)*" });
            sp.Children.Add(volumeBox);
            sp.Children.Add(saveBtn);

            win.Content = sp;
            win.Owner = Application.Current.MainWindow;

            saveBtn.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(codeBox.Text) ||
                    !decimal.TryParse(weightBox.Text, out var w) || w <= 0 ||
                    !decimal.TryParse(volumeBox.Text, out var v) || v <= 0)
                {
                    MessageBox.Show("Проверьте корректность данных.");
                    return;
                }

                var sel = (dynamic)whBox.SelectedItem;
                using var db = new PractikDbContext();
                db.StorageLocations.Add(new StorageLocation
                {
                    WarehouseId = sel.Id,
                    Code = codeBox.Text.Trim(),
                    MaxWeightKg = w,
                    MaxVolumeM3 = v,
                    IsAvailable = true
                });
                await db.SaveChangesAsync();

                await LoadAsync(); 

                win.Close();
                MessageBox.Show("Место хранения добавлено.");
            };

            win.ShowDialog();
        }

        private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Grid.SelectedItem is StorageLocation loc)
            {
                NavigationService.Navigate(new LocationDetailsPage(loc.LocationId));
            }
        }
    }
}