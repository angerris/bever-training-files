using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Work_Order_Management
{
    public class WorkOrderAutoNumberPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                if (targetEntity.LogicalName != "cr8c9_work_order")
                {
                    return;
                }

                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                int workOrderCount = GetWorkOrderCount(service);
                string workOrderNumber = $"WO-{(workOrderCount + 1):D4}";

                if (!string.IsNullOrEmpty(workOrderNumber))
                {
                    targetEntity["cr8c9_name"] = workOrderNumber;
                }
            }
        }

        private int GetWorkOrderCount(IOrganizationService service)
        {
            string fetchXml = @"
                <fetch aggregate='true'>
                    <entity name='cr8c9_work_order'>
                        <attribute name='cr8c9_work_orderid' alias='workOrderCount' aggregate='count' />
                    </entity>
                </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            return (int)(result.Entities.Count > 0 ? ((AliasedValue)result.Entities[0]["workOrderCount"]).Value : 0);
        }
    }
}
