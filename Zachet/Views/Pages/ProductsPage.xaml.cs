using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;
using Zachet.Views.Pages;

namespace Zachet.Views.Pages
{
    public partial class ProductsPage : Page
    {
        
        
        public ProductsPage()
        {
            InitializeComponent();

            Loaded += ProductsPage_Loaded;
        }

        private async void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
            }

            
        }

        private async Task LoadAsync()
        {
            using var db = new PractikDbContext();
            var products = await db.Products.ToListAsync();
            Grid.ItemsSource = products;
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int productId)
                return;

            var confirm = MessageBox.Show(
                "Удалить товар? Все связанные данные (остатки, перемещения, заказы) будут удалены или станут некорректными.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = new PractikDbContext();

                // Проверка: есть ли остатки?
                if (await db.Inventories.AnyAsync(i => i.ProductId == productId))
                {
                    MessageBox.Show("Нельзя удалить: товар есть на складе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка: есть ли в заказах?
                if (await db.OrderItems.AnyAsync(oi => oi.ProductId == productId))
                {
                    MessageBox.Show("Нельзя удалить: товар есть в заказах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка: есть ли в перемещениях или инвентаризациях?
                // (опционально — можно разрешить, если это исторические данные)

                var product = await db.Products.FindAsync(productId);
                if (product == null) return;

                db.Products.Remove(product);
                await db.SaveChangesAsync();

                db.ActionLogs.Add(new ActionLog
                {
                    ActionType = "Delete",
                    Entity = "Product",
                    EntityId = productId,
                    Comment = $"Удалён товар: {product.Name}"
                });
                await db.SaveChangesAsync();

                await LoadAsync(); // Обновить таблицу
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddProduct_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductEditPage(null));
        }
    }
}