using System.Windows;
using Zachet.Views.Pages;

namespace Zachet
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new DashboardPage());
        }

        private void GoToDashboard(object sender, RoutedEventArgs e) => MainFrame.Navigate(new DashboardPage());
        private void GoToProducts(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ProductsPage());
        private void GoToWarehouses(object sender, RoutedEventArgs e) => MainFrame.Navigate(new WarehousesPage());
        private void GoToLocations(object sender, RoutedEventArgs e) => MainFrame.Navigate(new StorageLocationsPage());
        private void GoToReceiving(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ReceivingPage());
        private void GoToMovements(object sender, RoutedEventArgs e) => MainFrame.Navigate(new MovementsPage());
        private void GoToPicking(object sender, RoutedEventArgs e) => MainFrame.Navigate(new PickingPage());

        private void GoToStorageLocations(object sender, RoutedEventArgs e) => MainFrame.Navigate(new StorageLocationsPage());
    }
}