using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace Work_Order_Management
{
    public class GenerateInvoice : CodeActivity
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
            var workOrder = service.Retrieve("cr8c9_work_order", workOrderRef.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("cr8c9_name", "cr8c9_fk_customer", "cr8c9_fk_contact", "cr8c9_fk_price_list", "cr8c9_mlot_description_of_work"));

            Guid newInvoiceId = CreateNewInvoice(service, workOrder);
            CreateInvoiceProducts(service, workOrderRef, newInvoiceId);
            CreateInvoiceServices(service, workOrderRef, newInvoiceId);
        }

        private Guid CreateNewInvoice(IOrganizationService service, Entity workOrder)
        {
            var workOrderNumber = workOrder.GetAttributeValue<string>("cr8c9_name");
            var invoiceNumber = workOrderNumber.Replace("WO-", "INV-");
            var descriptionOfWork = workOrder.GetAttributeValue<string>("cr8c9_mlot_description_of_work");
            var customer = workOrder.GetAttributeValue<EntityReference>("cr8c9_fk_customer");
            var contact = workOrder.GetAttributeValue<EntityReference>("cr8c9_fk_contact");
            var priceList = workOrder.GetAttributeValue<EntityReference>("cr8c9_fk_price_list");

            var invoice = new Entity("cr8c9_invoice")
            {
                ["cr8c9_name"] = invoiceNumber,
                ["cr8c9_fk_customer"] = customer,
                ["cr8c9_fk_contact"] = contact,
                ["cr8c9_fk_price_list"] = priceList,
                ["cr8c9_fk_work_order"] = new EntityReference("cr8c9_work_order", workOrder.Id),
                ["cr8c9_mlot_description_of_work"] = descriptionOfWork
            };

            return service.Create(invoice);
        }

        private void CreateInvoiceProducts(IOrganizationService service, EntityReference workOrderRef, Guid invoiceId)
        {
            var productQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("cr8c9_work_order_product")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("cr8c9_int_quantity", "cr8c9_fk_product", "cr8c9_mon_price_per_unit"),
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                {
                    Conditions = {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("cr8c9_fk_work_order", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, workOrderRef.Id)
                    }
                }
            };

            var workOrderProducts = service.RetrieveMultiple(productQuery).Entities;
            foreach (var product in workOrderProducts)
            {
                var quantity = product.GetAttributeValue<int>("cr8c9_int_quantity");
                var productRef = product.GetAttributeValue<EntityReference>("cr8c9_fk_product");
                var pricePerUnit = product.GetAttributeValue<Money>("cr8c9_mon_price_per_unit");

                var totalAmount = new Money(quantity * pricePerUnit.Value);

                var invoiceProduct = new Entity("cr8c9_invoice_product")
                {
                    ["cr8c9_fk_product"] = productRef,
                    ["cr8c9_int_quantity"] = quantity,
                    ["cr8c9_mon_price_per_unit"] = pricePerUnit,
                    ["cr8c9_mon_total_amount"] = totalAmount,
                    ["cr8c9_fk_invoice"] = new EntityReference("cr8c9_invoice", invoiceId)
                };

                service.Create(invoiceProduct);
            }
        }

        private void CreateInvoiceServices(IOrganizationService service, EntityReference workOrderRef, Guid invoiceId)
        {
            var serviceQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("cr8c9_work_order_service")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("cr8c9_int_duration", "cr8c9_fk_service", "cr8c9_mon_price_per_unit"),
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                {
                    Conditions = {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("cr8c9_fk_work_order", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, workOrderRef.Id)
                    }
                }
            };

            var workOrderServices = service.RetrieveMultiple(serviceQuery).Entities;
            foreach (var serviceItem in workOrderServices)
            {
                var durationInMinutes = serviceItem.GetAttributeValue<int>("cr8c9_int_duration");
                var serviceRef = serviceItem.GetAttributeValue<EntityReference>("cr8c9_fk_service");
                var pricePerHour = serviceItem.GetAttributeValue<Money>("cr8c9_mon_price_per_unit");

                decimal durationInHours = durationInMinutes / 60m;
                decimal totalAmount = Math.Round(durationInHours * pricePerHour.Value, 2);
                var totalAmountMoney = new Money(totalAmount);

                var invoiceServiceProduct = new Entity("cr8c9_invoice_product")
                {
                    ["cr8c9_fk_product"] = serviceRef,
                    ["cr8c9_int_duration"] = durationInMinutes,
                    ["cr8c9_mon_price_per_unit"] = pricePerHour,
                    ["cr8c9_mon_total_amount"] = totalAmountMoney,
                    ["cr8c9_fk_invoice"] = new EntityReference("cr8c9_invoice", invoiceId)
                };

                service.Create(invoiceServiceProduct);
            }
        }
    }
}
