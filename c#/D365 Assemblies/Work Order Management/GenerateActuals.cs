using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace Work_Order_Management
{
    public class GenerateActuals : CodeActivity
    {
        [Output("Status")]
        public OutArgument<string> Status { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            IExecutionContext crmContext = context.GetExtension<IExecutionContext>();

            if (!crmContext.InputParameters.Contains("Target") || !(crmContext.InputParameters["Target"] is EntityReference workOrderRef))
            {
                Status.Set(context, "Target is not set or is not a valid EntityReference.");
                return;
            }

            Guid workOrderId = workOrderRef.Id;

            DeleteRelatedActuals(service, workOrderId);

            var workOrderProducts = RetrieveWorkOrderProducts(service, workOrderId);
            if (workOrderProducts == null || workOrderProducts.Entities.Count == 0)
            {
                Status.Set(context, "No work order products found.");
                return;
            }

            var workOrderServices = RetrieveWorkOrderServices(service, workOrderId);
            if (workOrderServices == null || workOrderServices.Entities.Count == 0)
            {
                Status.Set(context, "No work order services found.");
                return;
            }

            foreach (var product in workOrderProducts.Entities)
            {
                CreateActualRecord(service, product, isProduct: true, workOrderId);
            }

            foreach (var serviceRecord in workOrderServices.Entities)
            {
                CreateActualRecord(service, serviceRecord, isProduct: false, workOrderId);
            }

            Status.Set(context, "Actuals generated successfully.");
        }

        private void DeleteRelatedActuals(IOrganizationService service, Guid workOrderId)
        {
            QueryExpression query = new QueryExpression("cr8c9_actual")
            {
                ColumnSet = new ColumnSet("cr8c9_actualid")
            };
            query.Criteria.AddCondition("cr8c9_fk_work_order", ConditionOperator.Equal, workOrderId);
            EntityCollection actuals = service.RetrieveMultiple(query);

            foreach (Entity actual in actuals.Entities)
            {
                service.Delete("cr8c9_actual", actual.Id);
            }
        }

        private EntityCollection RetrieveWorkOrderProducts(IOrganizationService service, Guid workOrderId)
        {
            QueryExpression query = new QueryExpression("cr8c9_work_order_product")
            {
                ColumnSet = new ColumnSet("cr8c9_fk_product", "cr8c9_int_quantity", "cr8c9_mon_price_per_unit")
            };
            query.Criteria.AddCondition("cr8c9_fk_work_order", ConditionOperator.Equal, workOrderId);
            return service.RetrieveMultiple(query);
        }

        private EntityCollection RetrieveWorkOrderServices(IOrganizationService service, Guid workOrderId)
        {
            QueryExpression query = new QueryExpression("cr8c9_work_order_service")
            {
                ColumnSet = new ColumnSet("cr8c9_fk_service", "cr8c9_int_duration", "cr8c9_mon_price_per_unit")
            };
            query.Criteria.AddCondition("cr8c9_fk_work_order", ConditionOperator.Equal, workOrderId);
            return service.RetrieveMultiple(query);
        }

        private void CreateActualRecord(IOrganizationService service, Entity record, bool isProduct, Guid workOrderId)
        {
            Entity actual = new Entity("cr8c9_actual");
            actual["cr8c9_fk_work_order"] = new EntityReference("cr8c9_work_order", workOrderId);

            if (isProduct)
            {
                if (record.Contains("cr8c9_fk_product") && record.Contains("cr8c9_int_quantity") && record.Contains("cr8c9_mon_price_per_unit"))
                {
                    actual["cr8c9_name"] = record.GetAttributeValue<EntityReference>("cr8c9_fk_product")?.Name;
                    actual["cr8c9_int_quantity"] = record.GetAttributeValue<int>("cr8c9_int_quantity");
                    int quantity = actual.GetAttributeValue<int>("cr8c9_int_quantity");
                    Money pricePerUnit = record.GetAttributeValue<Money>("cr8c9_mon_price_per_unit");

                    if (pricePerUnit != null)
                    {
                        int costPerUnit = (int)pricePerUnit.Value;
                        actual["cr8c9_mon_total_cost"] = new Money(quantity * costPerUnit);
                    }
                }
            }
            else
            {
                if (record.Contains("cr8c9_fk_service") && record.Contains("cr8c9_int_duration") && record.Contains("cr8c9_mon_price_per_unit"))
                {
                    actual["cr8c9_name"] = record.GetAttributeValue<EntityReference>("cr8c9_fk_service")?.Name;
                    actual["cr8c9_int_duration"] = record.GetAttributeValue<int>("cr8c9_int_duration");
                    int durationInMinutes = actual.GetAttributeValue<int>("cr8c9_int_duration");
                    Money pricePerUnit = record.GetAttributeValue<Money>("cr8c9_mon_price_per_unit");

                    if (pricePerUnit != null)
                    {
                        int costPerUnit = (int)pricePerUnit.Value;
                        decimal durationInHours = durationInMinutes / 60m;
                        decimal totalAmount = Math.Round(durationInHours * costPerUnit, 2);
                        actual["cr8c9_mon_total_cost"] = new Money(totalAmount);
                    }
                }
            }

            service.Create(actual);
        }
    }
}
