using Microsoft.Xrm.Tooling.Connector;

namespace Test_Connector.Utilities
{
     class D365Connector
    {
       private static string clientId = "3840b01c-7b1c-4f37-9bdd-79a2a3722c86";
       private static string clientSecret = "n458Q~Rb.tU0TsJMok.fALBIAJrHbS0ihrkAocxS";
       private static string authority = "https://login.microsoftonline.com/91b242cb-658d-4278-bd71-2c38fde7444c";
       private static string crmUrl = "https://org0f359662.crm4.dynamics.com/";
       private static string conString = $"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};Authority={authority};RequireNewInstance=True";
        public static CrmServiceClient GetServiceClient()
        {
            return new CrmServiceClient(conString);
        }
    }
}
