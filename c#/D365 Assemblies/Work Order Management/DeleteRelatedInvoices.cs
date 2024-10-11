using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace Work_Order_Management
{
    public class DeleteRelatedInvoices : CodeActivity
    {
        [Input("Work Order")]
        [ReferenceTarget("cr8c9_work_order")]
        public InArgument<EntityReference> WorkOrder { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            EntityReference workOrderRef = WorkOrder.Get(context);

            var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("cr8c9_invoice")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("cr8c9_invoiceid"),
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                {
                    Conditions = {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("cr8c9_fk_work_order", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, workOrderRef.Id)
                    }
                }
            };

            var relatedInvoices = service.RetrieveMultiple(query).Entities;
            foreach (var invoice in relatedInvoices)
            {
                service.Delete("cr8c9_invoice", invoice.Id);
            }
        }
    }
}
