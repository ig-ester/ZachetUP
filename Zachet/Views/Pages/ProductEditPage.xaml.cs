using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Zachet.Data;
using Zachet.Models;

namespace Zachet.Views.Pages
{
    public partial class ProductEditPage : Page
    {
        private readonly Product? _productToEdit;

        public ProductEditPage(Product? product)
        {
            InitializeComponent();
            _productToEdit = product;

            if (_productToEdit != null)
            {
                TitleText.Text = "Редактирование товара";
                SkuBox.Text = _productToEdit.SKU;
                NameBox.Text = _productToEdit.Name;
                DescBox.Text = _productToEdit.Description ?? string.Empty;
                WeightBox.Text = _productToEdit.WeightKg.ToString(CultureInfo.InvariantCulture);
                VolumeBox.Text = _productToEdit.VolumeM3.ToString(CultureInfo.InvariantCulture);
                ShelfLifeBox.Text = _productToEdit.ShelfLifeDays?.ToString() ?? string.Empty;
                StorageBox.Text = _productToEdit.StorageConditions ?? string.Empty;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            var sku = SkuBox.Text.Trim();
            var name = NameBox.Text.Trim();
            var desc = DescBox.Text.Trim();
            var weightText = WeightBox.Text.Trim();
            var volumeText = VolumeBox.Text.Trim();
            var shelfLifeText = ShelfLifeBox.Text.Trim();
            var storage = StorageBox.Text.Trim();

            if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Поля «Артикул» и «Наименование» обязательны.";
                return;
            }

            if (!decimal.TryParse(weightText, NumberStyles.Any, CultureInfo.InvariantCulture, out var weight) || weight <= 0)
            {
                ErrorText.Text = "Некорректный вес (должен быть положительным числом).";
                return;
            }

            if (!decimal.TryParse(volumeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume) || volume <= 0)
            {
                ErrorText.Text = "Некорректный объём (должен быть положительным числом).";
                return;
            }

            int? shelfLife = null;
            if (!string.IsNullOrEmpty(shelfLifeText))
            {
                if (!int.TryParse(shelfLifeText, out var sl) || sl <= 0)
                {
                    ErrorText.Text = "Срок годности должен быть положительным целым числом или пустым.";
                    return;
                }
                shelfLife = sl;
            }

            using var context = new PractikDbContext();

            if (_productToEdit == null)
            {
                var newProduct = new Product
                {
                    SKU = sku,
                    Name = name,
                    Description = string.IsNullOrEmpty(desc) ? null : desc,
                    WeightKg = weight,
                    VolumeM3 = volume,
                    ShelfLifeDays = shelfLife,
                    StorageConditions = string.IsNullOrEmpty(storage) ? null : storage
                };

                context.Products.Add(newProduct);
                context.SaveChanges();

                context.ActionLogs.Add(new ActionLog
                {
                    ActionType = "Create",
                    Entity = "Product",
                    EntityId = newProduct.ProductId,
                    Comment = $"Создан товар: {newProduct.Name}"
                });
                context.SaveChanges();

                MessageBox.Show("Товар успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var existing = context.Products.Find(_productToEdit.ProductId);
                if (existing != null)
                {
                    var oldName = existing.Name;
                    existing.SKU = sku;
                    existing.Name = name;
                    existing.Description = string.IsNullOrEmpty(desc) ? null : desc;
                    existing.WeightKg = weight;
                    existing.VolumeM3 = volume;
                    existing.ShelfLifeDays = shelfLife;
                    existing.StorageConditions = string.IsNullOrEmpty(storage) ? null : storage;

                    context.SaveChanges();

                    context.ActionLogs.Add(new ActionLog
                    {
                        ActionType = "Update",
                        Entity = "Product",
                        EntityId = existing.ProductId,
                        Comment = $"Обновлён товар: {oldName} → {name}"
                    });
                    context.SaveChanges();

                    MessageBox.Show("Товар успешно обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}