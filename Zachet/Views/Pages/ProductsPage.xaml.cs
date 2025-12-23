using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
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

        private void AddProduct_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductEditPage(null));
        }
    }
}