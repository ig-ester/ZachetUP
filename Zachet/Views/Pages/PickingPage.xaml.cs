using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class PickingPage : Page
    {
        private readonly ObservableCollection<OrderItem> _items = new();

        public PickingPage()
        {
            InitializeComponent();
            ItemsGrid.ItemsSource = _items;
            LoadProducts();
        }

        private async void LoadProducts()
        {
            using var db = new PractikDbContext();
            ProductCombo.ItemsSource = await db.Products.ToListAsync();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProductCombo.SelectedItem == null)
            {
                ErrorText.Text = "Выберите товар.";
                return;
            }

            if (!int.TryParse(QtyBox.Text, out var qty) || qty <= 0)
            {
                ErrorText.Text = "Некорректное количество.";
                return;
            }

            var product = (Product)ProductCombo.SelectedItem;
            _items.Add(new OrderItem { Product = product, QuantityRequested = qty });
            QtyBox.Text = "1";
        }

        private async void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CustomerBox.Text))
            {
                ErrorText.Text = "Укажите клиента.";
                return;
            }

            if (_items.Count == 0)
            {
                ErrorText.Text = "Добавьте хотя бы одну позицию.";
                return;
            }

            using var db = new PractikDbContext();

            foreach (var item in _items)
            {
                var totalAvailable = await db.Inventories
                    .Where(i => i.ProductId == item.Product.ProductId)
                    .SumAsync(i => i.Quantity - i.Reserved);

                if (totalAvailable < item.QuantityRequested)
                {
                    ErrorText.Text = $"Недостаточно товара: {item.Product.Name}";
                    return;
                }
            }

            var order = new Order { CustomerName = CustomerBox.Text };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            foreach (var item in _items)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.Product.ProductId,
                    QuantityRequested = item.QuantityRequested,
                    QuantityPicked = 0
                };
                db.OrderItems.Add(orderItem);

                var needed = item.QuantityRequested;
                var inventories = await db.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.ProductId == item.Product.ProductId && i.Quantity - i.Reserved > 0)
                    .OrderBy(i => i.ExpiryDate)
                    .ToListAsync();

                foreach (var inv in inventories)
                {
                    if (needed <= 0) break;
                    var canReserve = Math.Min(needed, inv.Quantity - inv.Reserved);
                    inv.Reserved += canReserve;
                    needed -= canReserve;
                }
            }

            db.ActionLogs.Add(new ActionLog
            {
                ActionType = "OrderCreated",
                Entity = "Order",
                EntityId = order.OrderId,
                Comment = $"Создан заказ {order.OrderNumber} для {order.CustomerName}"
            });

            await db.SaveChangesAsync();
            MessageBox.Show($"Заказ {order.OrderNumber} создан. Товары зарезервированы.");
            _items.Clear();
        }
    }
}