using Microsoft.EntityFrameworkCore;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class LocationDetailsPage : Page
    {
        private readonly int _locationId;

        public LocationDetailsPage(int locationId)
        {
            InitializeComponent();
            _locationId = locationId;

            Loaded += async (sender, args) =>
            {
                try
                {
                    await LoadAsync();
                }
                catch (System.Exception ex)
                {
                    TitleText.Text = "Ошибка загрузки данных";
                }
            };
        }

        private async Task LoadAsync()
        {
            using var db = new PractikDbContext();

            var loc = await db.StorageLocations
                .Include(l => l.Warehouse)
                .FirstOrDefaultAsync(l => l.LocationId == _locationId);

            if (loc != null)
            {
                TitleText.Text = $"Содержимое ячейки {loc.Code} (склад: {loc.Warehouse.Name})";
            }
            else
            {
                TitleText.Text = "Ячейка не найдена";
                Grid.ItemsSource = null;
                return;
            }

            var inventory = await db.Inventories
                .Include(i => i.Product)
                .Where(i => i.LocationId == _locationId)
                .ToListAsync();

            Grid.ItemsSource = inventory;
        }
    }
}