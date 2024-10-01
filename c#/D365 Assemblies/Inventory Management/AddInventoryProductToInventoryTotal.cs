using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Inventory_Management
{
    public class AddInventoryProductToInventoryTotal : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity inventoryProduct)
            {
                if (inventoryProduct.LogicalName != "cr8c9_inventory_product") return;

                EntityReference inventoryRef = inventoryProduct.GetAttributeValue<EntityReference>("cr8c9_fk_inventory");

                if (inventoryRef != null)
                {
                    decimal totalAmountSum = 0;
                    QueryExpression query = new QueryExpression("cr8c9_inventory_product")
                    {
                        ColumnSet = new ColumnSet("cr8c9_mon_total_amount"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("cr8c9_fk_inventory", ConditionOperator.Equal, inventoryRef.Id)
                            }
                        }
                    };

                    EntityCollection inventoryProducts = service.RetrieveMultiple(query);

                    foreach (var product in inventoryProducts.Entities)
                    {
                        Money productTotalMoney = product.GetAttributeValue<Money>("cr8c9_mon_total_amount");
                        totalAmountSum += productTotalMoney?.Value ?? 0;
                    }

                    Entity inventory = service.Retrieve(inventoryRef.LogicalName, inventoryRef.Id, new ColumnSet("cr8c9_mon_total_amount"));
                    inventory["cr8c9_mon_total_amount"] = new Money(totalAmountSum);
                    service.Update(inventory);
                }
            }
        }
    }
}
