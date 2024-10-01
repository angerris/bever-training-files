using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Customer_Asset_Management
{
    public class AssetAutoNumberPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                if (targetEntity.LogicalName != "cr8c9_customer_asset") return;

                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                if (targetEntity.Contains("cr8c9_fk_account") && targetEntity["cr8c9_fk_account"] is EntityReference)
                {
                    EntityReference accountRef = (EntityReference)targetEntity["cr8c9_fk_account"];
                    Guid accountId = accountRef.Id;

                    Entity account = service.Retrieve("cr8c9_account", accountId, new ColumnSet("cr8c9_name"));
                    string companyName = account.Contains("cr8c9_name") ? account["cr8c9_name"].ToString() : null;

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        QueryExpression query = new QueryExpression("cr8c9_customer_asset")
                        {
                            ColumnSet = new ColumnSet("cr8c9_name"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("cr8c9_fk_account", ConditionOperator.Equal, accountId)
                                }
                            }
                        };

                        EntityCollection assets = service.RetrieveMultiple(query);
                        int assetCount = assets.Entities.Count + 1;

                        string assetNumber = $"{companyName}-{assetCount:D4}";

                        targetEntity["cr8c9_name"] = assetNumber;
                    }
                }
            }
        }
    }
}
