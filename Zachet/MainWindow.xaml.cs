using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet
{
    public partial class MainWindow : Window
    {
        private readonly PractikDbContext _context;

        public MainWindow()
        {
            InitializeComponent();
            _context = new PractikDbContext();
            LoadProducts();
        }

        private void LoadProducts()
        {
            var products = _context.Products
                .AsNoTracking()
                .ToList();
            ProductsDataGrid.ItemsSource = products;
        }

        private void BtnCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateOrderWindow();
            window.ShowDialog();
            LoadProducts();
        }

        private void BtnStorage_Click(object sender, RoutedEventArgs e)
        {
            var window = new StorageManagementWindow();
            window.ShowDialog();
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddProductWindow();
            if (window.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int productId)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить этот товар? Все связанные данные могут быть потеряны.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var product = _context.Products.Find(productId);
                    if (product != null)
                    {
                        var usedInStorage = _context.StorageLocations.Any(s => s.ProductId == productId);
                        if (usedInStorage)
                        {
                            MessageBox.Show("Нельзя удалить товар, который находится на складе!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var usedInOrders = _context.OrderDetails.Any(od => od.ProductId == productId);
                        if (usedInOrders)
                        {
                            MessageBox.Show("Нельзя удалить товар, который есть в заказах!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        _context.Products.Remove(product);
                        _context.SaveChanges();
                        LoadProducts();
                    }
                }
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}