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

            var (resource, startDate, endDate) = GetBookingDetails(service, booking);
            ValidateBookingDates(startDate, endDate);
            CheckForBookingConflict(service, booking.Id, resource.Id, startDate.Value, endDate.Value);
        }

        private (EntityReference resource, DateTime? startDate, DateTime? endDate) GetBookingDetails(IOrganizationService service, Entity booking)
        {
            EntityReference resource = booking.Contains("cr8c9_fk_resource") ? (EntityReference)booking["cr8c9_fk_resource"] : null;
            DateTime? startDate = booking.Contains("cr8c9_dt_start_date") ? booking.GetAttributeValue<DateTime>("cr8c9_dt_start_date") : (DateTime?)null;
            DateTime? endDate = booking.Contains("cr8c9_dt_end_date") ? booking.GetAttributeValue<DateTime>("cr8c9_dt_end_date") : (DateTime?)null;

            if (resource == null || !startDate.HasValue || !endDate.HasValue)
            {
                booking = service.Retrieve("cr8c9_booking", booking.Id, new ColumnSet("cr8c9_fk_resource", "cr8c9_dt_start_date", "cr8c9_dt_end_date"));
                resource = resource ?? booking.GetAttributeValue<EntityReference>("cr8c9_fk_resource");
                startDate = startDate ?? booking.GetAttributeValue<DateTime>("cr8c9_dt_start_date");
                endDate = endDate ?? booking.GetAttributeValue<DateTime>("cr8c9_dt_end_date");
            }

            return (resource, startDate, endDate);
        }

        private void ValidateBookingDates(DateTime? startDate, DateTime? endDate)
        {
            if (startDate > endDate)
            {
                throw new InvalidPluginExecutionException("The start date must be earlier than the end date.");
            }
        }

        private void CheckForBookingConflict(IOrganizationService service, Guid bookingId, Guid resourceId, DateTime startDate, DateTime endDate)
        {
            QueryExpression query = new QueryExpression("cr8c9_booking")
            {
                ColumnSet = new ColumnSet("cr8c9_dt_start_date", "cr8c9_dt_end_date"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_fk_resource", ConditionOperator.Equal, resourceId),
                        new ConditionExpression("cr8c9_bookingid", ConditionOperator.NotEqual, bookingId),
                        new ConditionExpression("cr8c9_dt_start_date", ConditionOperator.LessEqual, endDate),
                        new ConditionExpression("cr8c9_dt_end_date", ConditionOperator.GreaterEqual, startDate)
                    }
                }
            };

            EntityCollection existingBookings = service.RetrieveMultiple(query);

            if (existingBookings.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("The booking conflicts with another existing booking for the same resource.");
            }
        }
    }
}
