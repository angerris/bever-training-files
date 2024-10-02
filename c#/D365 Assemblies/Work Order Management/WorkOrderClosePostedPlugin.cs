using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Work_Order_Management
{
    public class WorkOrderClosePostedPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.MessageName != "Update" || context.PrimaryEntityName != "cr8c9_work_order")
                return;

            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity workOrder)
            {
                if (workOrder.Contains("cr8c9_os_status"))
                {
                    OptionSetValue status = workOrder.GetAttributeValue<OptionSetValue>("cr8c9_os_status");
                    if (status != null && status.Value == 976090001)
                    {
                        List<Entity> workOrderProducts = RetrieveWorkOrderProducts(service, context.PrimaryEntityId);
                        foreach (var product in workOrderProducts)
                        {
                            SubtractQuantityFromInventory(service, product);
                        }
                    }
                }
            }
        }

        private List<Entity> RetrieveWorkOrderProducts(IOrganizationService service, Guid workOrderId)
        {
            QueryExpression query = new QueryExpression("cr8c9_work_order_product")
            {
                ColumnSet = new ColumnSet("cr8c9_int_quantity", "cr8c9_fk_product"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_fk_work_order", ConditionOperator.Equal, workOrderId)
                    }
                }
            };

            EntityCollection results = service.RetrieveMultiple(query);
            return new List<Entity>(results.Entities);
        }

        private void SubtractQuantityFromInventory(IOrganizationService service, Entity workOrderProduct)
        {
            EntityReference productRef = workOrderProduct.GetAttributeValue<EntityReference>("cr8c9_fk_product");

            Guid productId = productRef.Id;
            int quantityToSubtract = workOrderProduct.GetAttributeValue<int>("cr8c9_int_quantity");

            QueryExpression inventoryProductQuery = new QueryExpression("cr8c9_inventory_product")
            {
                ColumnSet = new ColumnSet("cr8c9_int_quantity"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_fk_product_", ConditionOperator.Equal, productId)
                    }
                }
            };

            EntityCollection inventoryProductResults = service.RetrieveMultiple(inventoryProductQuery);
            if (inventoryProductResults.Entities.Count > 0)
            {
                Entity inventoryProductRecord = inventoryProductResults.Entities[0];
                int currentQuantity = inventoryProductRecord.GetAttributeValue<int>("cr8c9_int_quantity");
                if (currentQuantity >= quantityToSubtract)
                {
                    int newQuantity = currentQuantity - quantityToSubtract;
                    inventoryProductRecord["cr8c9_int_quantity"] = newQuantity;
                    service.Update(inventoryProductRecord);
                }
            }
        }
    }
}
