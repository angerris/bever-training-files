using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Inventory_Management
{
    public class RemoveInventoryProductFromInventoryTotal : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth > 1) return;
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference inventoryProductRef)
            {
                if (inventoryProductRef.LogicalName != "cr8c9_inventory_product") return;
                if (context.Stage != 40) return;
                if (context.PreEntityImages.Contains("PreImage"))
                {
                    Entity preImage = (Entity)context.PreEntityImages["PreImage"];
                    EntityReference inventoryRef = preImage.GetAttributeValue<EntityReference>("cr8c9_fk_inventory");
                    if (inventoryRef == null) return;
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

                    Entity inventory = new Entity(inventoryRef.LogicalName, inventoryRef.Id)
                    {
                        ["cr8c9_mon_total_amount"] = new Money(totalAmountSum)
                    };

                    service.Update(inventory);
                }
            }
        }
    }
}
