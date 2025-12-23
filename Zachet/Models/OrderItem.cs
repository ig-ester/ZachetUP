using System.ComponentModel.DataAnnotations;
using Zachet.Models;

namespace Zachet.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int QuantityRequested { get; set; }
        public int QuantityPicked { get; set; }
    }
}