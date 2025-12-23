using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage() => InitializeComponent();

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAggregatedInventoryAsync();
        }

        private async Task LoadAggregatedInventoryAsync()
        {
            using var db = new PractikDbContext();
            var report = await db.Inventories
                .Include(i => i.Product)
                .Include(i => i.Location)
                    .ThenInclude(l => l.Warehouse)
                .Where(i => i.Quantity > 0)
                .GroupBy(i => new { i.ProductId, i.LocationId })
                .Select(g => new
                {
                    ProductName = g.First().Product.Name,
                    SKU = g.First().Product.SKU,
                    WarehouseName = g.First().Location.Warehouse.Name,
                    LocationCode = g.First().Location.Code,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalWeight = g.Sum(i => i.Quantity * i.Product.WeightKg),
                    TotalVolume = g.Sum(i => i.Quantity * i.Product.VolumeM3)
                })
                .OrderBy(x => x.WarehouseName)
                .ThenBy(x => x.LocationCode)
                .ThenBy(x => x.ProductName)
                .ToListAsync();

            InventoryGrid.ItemsSource = report;
        }
}
}