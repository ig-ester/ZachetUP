using System.ComponentModel.DataAnnotations;
namespace Zachet.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        [Required] public string SKU { get; set; } = string.Empty;
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Range(0.001, double.MaxValue)] public decimal WeightKg { get; set; }
        [Range(0.001, double.MaxValue)] public decimal VolumeM3 { get; set; }
        public int? ShelfLifeDays { get; set; }
        public string? StorageConditions { get; set; }
    }
}