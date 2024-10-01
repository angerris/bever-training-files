using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Booking_Management
{
    public class BookingConflictPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.MessageName != "Create" && context.MessageName != "Update")
            {
                return;
            }

            Entity booking = (Entity)context.InputParameters["Target"];
            if (booking == null)
            {
                throw new InvalidPluginExecutionException("No target entity.");
            }

            if (!booking.Contains("cr8c9_fk_resource") || !booking.Contains("cr8c9_dt_start_date") || !booking.Contains("cr8c9_dt_end_date"))
            {
                throw new InvalidPluginExecutionException("Booking is missing required fields.");
            }

            EntityReference resource = (EntityReference)booking["cr8c9_fk_resource"];
            DateTime startDate = booking.GetAttributeValue<DateTime>("cr8c9_dt_start_date");
            DateTime endDate = booking.GetAttributeValue<DateTime>("cr8c9_dt_end_date");

            if (startDate >= endDate)
            {
                throw new InvalidPluginExecutionException("The start date must be earlier than the end date.");
            }

            QueryExpression query = new QueryExpression("cr8c9_booking")
            {
                ColumnSet = new ColumnSet("cr8c9_dt_start_date", "cr8c9_dt_end_date"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_fk_resource", ConditionOperator.Equal, resource.Id),
                        new ConditionExpression("cr8c9_bookingid", ConditionOperator.NotEqual, booking.Id),
                        new ConditionExpression("cr8c9_dt_start_date", ConditionOperator.LessEqual, endDate),
                        new ConditionExpression("cr8c9_dt_end_date", ConditionOperator.GreaterEqual, startDate)
                    }
                }
            };

            EntityCollection existingBookings = service.RetrieveMultiple(query);

            if (existingBookings.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("The booking conflicts with another existing booking for the same person.");
            }
        }
    }
}
