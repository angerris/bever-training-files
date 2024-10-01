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

                    EntityCollection inventoryProducts = service.RetrieveMultiple(inventoryProductQuery);

                    foreach (Entity inventoryProduct in inventoryProducts.Entities)
                    {
                        if (inventoryProduct.Contains("cr8c9_fk_product_") && inventoryProduct["cr8c9_fk_product_"] is EntityReference)
                        {
                            EntityReference productRef = (EntityReference)inventoryProduct["cr8c9_fk_product_"];

                            QueryExpression priceListItemQuery = new QueryExpression("cr8c9_price_list_item")
                            {
                                ColumnSet = new ColumnSet("cr8c9_mon_price"),
                                Criteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("cr8c9_fk_price_list", ConditionOperator.Equal, newPriceListRef.Id),
                                        new ConditionExpression("cr8c9_fk_product", ConditionOperator.Equal, productRef.Id)
                                    }
                                }
                            };

                            EntityCollection priceListItems = service.RetrieveMultiple(priceListItemQuery);

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
    }
}
