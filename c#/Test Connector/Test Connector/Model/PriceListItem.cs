using System;

namespace Test_Connector.Model
{
     class PriceListItem
    {
        public Guid priceListId { get; set; }
        public Guid productId { get; set; }
        public Guid transactionCurrencyId { get; set; }
        public decimal pricePerUnit { get; set; }


    }
}
