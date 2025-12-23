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
                    await LoadAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки складов: {ex.Message}");
                }
            };
        }

        private async Task LoadAsync()
        {
            using var db = new PractikDbContext();
            Grid.ItemsSource = await db.Warehouses.ToListAsync();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window
            {
                Title = "Новый склад",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            var nameBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var addrBox = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            var saveBtn = new Button { Content = "Сохранить", Width = 100, Height = 30 };

            sp.Children.Add(new TextBlock { Text = "Название*" });
            sp.Children.Add(nameBox);
            sp.Children.Add(new TextBlock { Text = "Адрес*" });
            sp.Children.Add(addrBox);
            sp.Children.Add(saveBtn);

            win.Content = sp;
            win.Owner = Application.Current.MainWindow;

            saveBtn.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(addrBox.Text))
                {
                    MessageBox.Show("Заполните все поля.");
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

                await LoadAsync();

                win.Close();
                MessageBox.Show("Склад добавлен.");
            };

            win.ShowDialog();
        }
    }
}