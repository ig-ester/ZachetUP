namespace Zachet.Models
{
    public class ReceivingItem
    {
        public Product Product { get; set; } = null!;
        public bool IsSelected { get; set; } = false;
        public int Quantity { get; set; } = 1;
    }
}