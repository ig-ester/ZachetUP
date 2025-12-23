using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zachet.Models
{
    internal class ReceivingItem
    {

            public Product Product { get; set; } = null!;
            public bool IsSelected { get; set; } = false;
            public int Quantity { get; set; } = 1;
            public StorageLocation? SelectedLocation { get; set; } = null;
    }
}
