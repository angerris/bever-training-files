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

                if (targetEntity.Contains("cr8c9_fk_account") && targetEntity["cr8c9_fk_account"] is EntityReference accountRef)
                {
                    Guid accountId = accountRef.Id;
                    string companyName = GetAccountName(service, accountId);

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        int assetCount = GetAssetCount(service, accountId);
                        string assetNumber = $"{companyName}-{(assetCount + 1):D4}";
                        targetEntity["cr8c9_name"] = assetNumber;
                    }
                }
            }
        }

        private string GetAccountName(IOrganizationService service, Guid accountId)
        {
            Entity account = service.Retrieve("cr8c9_account", accountId, new ColumnSet("cr8c9_name"));
            return account.Contains("cr8c9_name") ? account["cr8c9_name"].ToString() : null;
        }

        private int GetAssetCount(IOrganizationService service, Guid accountId)
        {
            string fetchXml = $@"
                <fetch aggregate='true'>
                    <entity name='cr8c9_customer_asset'>
                        <attribute name='cr8c9_customer_assetid' alias='assetCount' aggregate='count' />
                        <filter>
                            <condition attribute='cr8c9_fk_account' operator='eq' value='{accountId}' />
                        </filter>
                    </entity>
                </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            return (int)(result.Entities.Count > 0 ? ((AliasedValue)result.Entities[0]["assetCount"]).Value : 0);
        }
    }
}
