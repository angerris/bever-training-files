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
                    Money totalAmountSum = GetTotalAmountSum(service, inventoryRef.Id);
                    Entity inventory = service.Retrieve(inventoryRef.LogicalName, inventoryRef.Id, new ColumnSet("cr8c9_mon_total_amount"));
                    inventory["cr8c9_mon_total_amount"] = totalAmountSum;
                    service.Update(inventory);
                }
            }
        }

        private Money GetTotalAmountSum(IOrganizationService service, Guid inventoryId)
        {
            string fetchXml = $@"
            <fetch aggregate='true'>
                <entity name='cr8c9_inventory_product'>
                    <attribute name='cr8c9_mon_total_amount' alias='TotalSum' aggregate='sum' />
                    <filter>
                        <condition attribute='cr8c9_fk_inventory' operator='eq' value='{inventoryId}' />
                    </filter>
                </entity>
            </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            decimal totalSumValue = 0;
            if (result.Entities.Count > 0)
            {
                AliasedValue totalSumAlias = result.Entities[0].GetAttributeValue<AliasedValue>("TotalSum");
                if (totalSumAlias != null && totalSumAlias.Value is Money moneyValue)
                {
                    totalSumValue = moneyValue.Value;
                }
            }

            return new Money(totalSumValue);
        }
    }
}
