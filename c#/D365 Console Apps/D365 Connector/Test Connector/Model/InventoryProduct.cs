using System;

namespace Test_Connector.Model
{
    public class InventoryProduct
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public Guid InventoryId { get; set; }
        public Guid InventoryProductId { get;  set; }
    }
}
