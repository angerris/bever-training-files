using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Inventory_Management
{
    public class UpdateInventoryTotalOnProductChange : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
       
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity inventoryProduct)
            {
                if (inventoryProduct.LogicalName != "cr8c9_inventory_product") return;
              
                    var inventoryProductId = inventoryProduct.Id;
                    var inventoryProductEntity = service.Retrieve(inventoryProduct.LogicalName, inventoryProductId, new ColumnSet("cr8c9_fk_inventory"));
                    EntityReference inventoryRef = inventoryProductEntity.GetAttributeValue<EntityReference>("cr8c9_fk_inventory");

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
                        <attribute name='cr8c9_mon_total_amount' alias='totalAmountSum' aggregate='sum'/>
                        <filter>
                            <condition attribute='cr8c9_fk_inventory' operator='eq' value='{inventoryId}' />
                        </filter>
                    </entity>
                </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            if (result.Entities.Count > 0)
            {
                var aliasedValue = result.Entities[0].GetAttributeValue<AliasedValue>("totalAmountSum");
                if (aliasedValue != null && aliasedValue.Value is Money moneyValue)
                {
                    return moneyValue.Value;
                }
               
            }

            return 0;
        }
    }
}
