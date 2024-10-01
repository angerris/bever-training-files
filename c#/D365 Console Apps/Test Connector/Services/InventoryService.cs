using System;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Test_Connector.Model;
using Microsoft.Xrm.Tooling.Connector;
using Test_Connector.Utilities;

namespace Test_Connector.Services
{
     class InventoryService

    {
        private static CrmServiceClient crmService = D365Connector.GetServiceClient();
        public void HandleSubtraction(Guid inventoryId, Guid productId, int quantity)
        {
            var inventoryProduct = GetInventoryProduct(inventoryId, productId);

            if (inventoryProduct == null)
            {
                Console.WriteLine("Product not found in the inventory.");
                return;
            }

            if (inventoryProduct.Quantity < quantity)
            {
                Console.WriteLine("Not enough product quantity in the inventory.");
                return;
            }

            if (inventoryProduct.Quantity == quantity)
            {
                DeleteInventoryProduct(inventoryProduct.InventoryProductId);
                Console.WriteLine("Product removed from inventory as quantity reached zero.");
            }
            else
            {
                inventoryProduct.Quantity -= quantity;
                UpdateInventoryProduct(inventoryProduct);
                Console.WriteLine("Product quantity updated successfully.");
            }
        }

        public void HandleAddition(Guid inventoryId, Guid productId, int quantity)
        {
            var inventoryProduct = GetInventoryProduct(inventoryId, productId);

            if (inventoryProduct != null)
            {
                inventoryProduct.Quantity += quantity;
                UpdateInventoryProduct(inventoryProduct);
                Console.WriteLine("Product quantity updated successfully.");
            }
            else
            {
                CreateNewInventoryProduct(inventoryId, productId, quantity);
                Console.WriteLine("New product added to inventory.");
            }
        }
        public Guid GetInventoryIdByName(string inventoryName)
        {
            string fetchXml = $@"
            <fetch top='1'>
              <entity name='cr8c9_inventory'>
                <attribute name='cr8c9_inventoryid' />
                <filter>
                  <condition attribute='cr8c9_name' operator='eq' value='{inventoryName}' />
                </filter>
              </entity>
            </fetch>";

            EntityCollection result = crmService.RetrieveMultiple(new FetchExpression(fetchXml));

            if (result.Entities.Count > 0)
            {
                return result.Entities[0].Id;
            }
            return Guid.Empty;
        }

        public Guid GetProductIdByName(string productName)
        {
            string fetchXml = $@"
            <fetch top='1'>
              <entity name='cr8c9_product'>
                <attribute name='cr8c9_productid' />
                <filter>
                  <condition attribute='cr8c9_name' operator='eq' value='{productName}' />
                </filter>
              </entity>
            </fetch>";

            EntityCollection result = crmService.RetrieveMultiple(new FetchExpression(fetchXml));

            if (result.Entities.Count > 0)
            {
                return result.Entities[0].Id;
            }
            return Guid.Empty;
        }
        private static void DeleteInventoryProduct(Guid inventoryProductId)
        {
            crmService.Delete("cr8c9_inventory_product", inventoryProductId);
        }

        private static InventoryProduct GetInventoryProduct(Guid inventoryId, Guid productId)
        {
            string fetchXml = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='cr8c9_inventory_product'>
                    <attribute name='cr8c9_inventory_productid'/>
                    <attribute name='cr8c9_fk_product_'/>
                    <attribute name='cr8c9_int_quantity'/> 
                    <filter type='and'>
                      <condition attribute='cr8c9_fk_inventory' operator='eq' value='{inventoryId}'/>
                    </filter>
                  </entity>
                </fetch>";
            EntityCollection inventoryProducts = crmService.RetrieveMultiple(new FetchExpression(fetchXml));
            foreach (Entity productEntity in inventoryProducts.Entities)
            {
                if (productEntity.GetAttributeValue<EntityReference>("cr8c9_fk_product_").Id == productId)
                {
                    return new InventoryProduct
                    {
                        InventoryProductId = productEntity.Id,
                        ProductId = productId,
                        InventoryId = inventoryId,
                        Quantity = productEntity.GetAttributeValue<int>("cr8c9_int_quantity")
                    };
                }
            }
            return null;
        }


        private static void UpdateInventoryProduct(InventoryProduct inventoryProduct)
        {
            Entity updateEntity = new Entity("cr8c9_inventory_product", inventoryProduct.InventoryProductId);
            updateEntity["cr8c9_int_quantity"] = inventoryProduct.Quantity;
            crmService.Update(updateEntity);
        }

        private static void CreateNewInventoryProduct(Guid inventoryId, Guid productId, int quantity)
        {
            Entity newInventoryProduct = new Entity("cr8c9_inventory_product");
            newInventoryProduct["cr8c9_fk_inventory"] = new EntityReference("cr8c9_inventory", inventoryId);
            newInventoryProduct["cr8c9_fk_product_"] = new EntityReference("product", productId);
            newInventoryProduct["cr8c9_int_quantity"] = quantity;
            Guid currency = GetCurrencyFromPriceList(inventoryId);
            Money pricePerUnit = GetPricePerUnit(productId);
            newInventoryProduct["transactioncurrencyid"] = new EntityReference("transactioncurrency", currency);
            newInventoryProduct["cr8c9_mon_price_per_unit"] = pricePerUnit;
            decimal totalPrice = pricePerUnit.Value > 0 ? pricePerUnit.Value * quantity : 1 * quantity;
            newInventoryProduct["cr8c9_mon_total_amount"] = new Money(totalPrice);
            crmService.Create(newInventoryProduct);
        }


        private static Guid GetCurrencyFromPriceList(Guid inventoryId)
        {
            string fetchXmlInventory = $@"
                    <fetch top='1'>
                      <entity name='cr8c9_inventory'>
                        <attribute name='cr8c9_fk_price_list' />
                        <filter>
                          <condition attribute='cr8c9_inventoryid' operator='eq' value='{inventoryId}' />
                        </filter>
                      </entity>
                    </fetch>";

            EntityCollection inventoryResult = crmService.RetrieveMultiple(new FetchExpression(fetchXmlInventory));

            if (inventoryResult.Entities.Count == 0)
            {
                Console.WriteLine("No Price List found for the specified inventory.");
                return Guid.Empty;
            }

            Guid priceListId = inventoryResult.Entities[0].GetAttributeValue<EntityReference>("cr8c9_fk_price_list").Id;
            string fetchXmlPriceList = $@"
                <fetch top='1'>
                  <entity name='cr8c9_price_list'>
                    <attribute name='transactioncurrencyid' />
                    <filter>
                      <condition attribute='cr8c9_price_listid' operator='eq' value='{priceListId}' />
                    </filter>
                  </entity>
                </fetch>";

            EntityCollection priceListResult = crmService.RetrieveMultiple(new FetchExpression(fetchXmlPriceList));

            if (priceListResult.Entities.Count > 0)
            {
                return priceListResult.Entities[0].GetAttributeValue<EntityReference>("transactioncurrencyid").Id;
            }

            return Guid.Empty;
        }


        private static Money GetPricePerUnit(Guid productId)
        {
            string fetchXml = $@"
            <fetch top='1'>
              <entity name='cr8c9_price_list_item'>
                <attribute name='cr8c9_mon_price' />
                <filter>
                  <condition attribute='cr8c9_fk_product' operator='eq' value='{productId}' />
                </filter>
              </entity>
            </fetch>";

            EntityCollection result = crmService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result.Entities.Count > 0)
            {
                return result.Entities[0].GetAttributeValue<Money>("cr8c9_mon_price");
            }
            return new Money(1);
        }

       
    }
}
