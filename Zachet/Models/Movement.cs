using System.ComponentModel.DataAnnotations;
using Zachet.Models;

namespace Zachet.Models
{
    public class Movement
    {
        [Key]
        public int MovementId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? FromLocationId { get; set; }
        public StorageLocation? FromLocation { get; set; }
        public int ToLocationId { get; set; }
        public StorageLocation ToLocation { get; set; } = null!;
        public int Quantity { get; set; }
        public string PerformedBy { get; set; } = Environment.UserName;
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
    }
}