using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Inventory_Management
{
    public class UpdatePricePerUnitOnPriceListChange : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity inventory = (Entity)context.InputParameters["Target"];

                if (inventory.Contains("cr8c9_fk_price_list") && inventory["cr8c9_fk_price_list"] is EntityReference)
                {
                    EntityReference newPriceListRef = (EntityReference)inventory["cr8c9_fk_price_list"];

                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    Guid inventoryId = inventory.Id;
                    EntityCollection inventoryProducts = RetrieveInventoryProducts(service, inventoryId);

                    foreach (Entity inventoryProduct in inventoryProducts.Entities)
                    {
                        if (inventoryProduct.Contains("cr8c9_fk_product_") && inventoryProduct["cr8c9_fk_product_"] is EntityReference)
                        {
                            EntityReference productRef = (EntityReference)inventoryProduct["cr8c9_fk_product_"];
                            EntityCollection priceListItems = RetrievePriceListItems(service, newPriceListRef.Id, productRef.Id);

                            if (priceListItems.Entities.Any())
                            {
                                Entity priceListItem = priceListItems.Entities.First();
                                decimal pricePerUnit = priceListItem.GetAttributeValue<Money>("cr8c9_mon_price")?.Value ?? 1;

                                inventoryProduct["cr8c9_mon_price_per_unit"] = new Money(pricePerUnit);
                                service.Update(inventoryProduct);
                            }
                        }
                    }
                }
            }
        }

        private EntityCollection RetrieveInventoryProducts(IOrganizationService service, Guid inventoryId)
        {
            QueryExpression inventoryProductQuery = new QueryExpression("cr8c9_inventory_product")
            {
                ColumnSet = new ColumnSet("cr8c9_fk_product_", "cr8c9_mon_price_per_unit"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_fk_inventory", ConditionOperator.Equal, inventoryId)
                    }
                }
            };

            return service.RetrieveMultiple(inventoryProductQuery);
        }

        private EntityCollection RetrievePriceListItems(IOrganizationService service, Guid priceListId, Guid productId)
        {
            QueryExpression priceListItemQuery = new QueryExpression("cr8c9_price_list_item")
            {
                ColumnSet = new ColumnSet("cr8c9_mon_price"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_fk_price_list", ConditionOperator.Equal, priceListId),
                        new ConditionExpression("cr8c9_fk_product", ConditionOperator.Equal, productId)
                    }
                }
            };

            return service.RetrieveMultiple(priceListItemQuery);
        }
    }
}
