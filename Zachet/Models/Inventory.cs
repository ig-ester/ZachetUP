using System.ComponentModel.DataAnnotations;
using Zachet.Models;

namespace Zachet.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int LocationId { get; set; }
        public StorageLocation Location { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public int Reserved { get; set; }
    }
}