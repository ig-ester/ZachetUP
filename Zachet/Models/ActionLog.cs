using System.ComponentModel.DataAnnotations;

namespace Zachet.Models
{
    public class ActionLog
    {
        [Key]
        public int LogId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Actor { get; set; } = Environment.UserName;
        public string ActionType { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int? FromLocationId { get; set; }
        public int? ToLocationId { get; set; }
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Comment { get; set; }
        public string? IPAddress { get; set; }
        public string? DeviceInfo { get; set; } = "WPF Desktop";
    }
}