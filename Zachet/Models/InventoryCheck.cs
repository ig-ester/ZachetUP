using System.ComponentModel.DataAnnotations;
using Zachet.Models;

namespace Zachet.Models
{
    public class InventoryCheck
    {
        [Key]
        public int CheckId { get; set; }
        public int LocationId { get; set; }
        public StorageLocation Location { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int SystemQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public int Discrepancy => ActualQuantity - SystemQuantity;
        public string CheckedBy { get; set; } = Environment.UserName;
        public DateTime CheckDate { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; }
    }
}