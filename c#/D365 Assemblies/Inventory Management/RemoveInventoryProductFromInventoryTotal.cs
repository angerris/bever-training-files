using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Inventory_Management
{
    public class RemoveInventoryProductFromInventoryTotal : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth > 1)
            {
                return;
            }

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference inventoryProductRef)
            {
                if (inventoryProductRef.LogicalName != "cr8c9_inventory_product")
                {
                    return;
                }

                EntityReference inventoryRef = inventoryProductRef;

                decimal totalAmountSum = GetTotalAmountSum(service, inventoryRef.Id);

                Entity inventory = new Entity(inventoryRef.LogicalName, inventoryRef.Id)
                {
                    ["cr8c9_mon_total_amount"] = new Money(totalAmountSum)
                };

                service.Update(inventory);
            }
        }

        private decimal GetTotalAmountSum(IOrganizationService service, Guid inventoryId)
        {
            string fetchXml = $@"
                <fetch aggregate='true'>
                    <entity name='cr8c9_inventory_product'>
                        <attribute name='cr8c9_mon_total_amount' alias='TotalAmountSum' aggregate='sum'/>
                        <filter>
                            <condition attribute='cr8c9_fk_inventory' operator='eq' value='{inventoryId}' />
                        </filter>
                    </entity>
                </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            if (result.Entities.Count > 0)
            {
                var aliasedValue = result.Entities[0].GetAttributeValue<AliasedValue>("TotalAmountSum");
                if (aliasedValue != null && aliasedValue.Value is decimal sumValue)
                {
                    return sumValue;
                }
            }

            return 0;
        }
    }
}
