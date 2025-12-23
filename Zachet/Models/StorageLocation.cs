using System.ComponentModel.DataAnnotations;
using Zachet.Models;

namespace Zachet.Models
{
    public class StorageLocation
    {
        [Key]
        public int LocationId { get; set; }
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public decimal MaxWeightKg { get; set; }
        public decimal MaxVolumeM3 { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}