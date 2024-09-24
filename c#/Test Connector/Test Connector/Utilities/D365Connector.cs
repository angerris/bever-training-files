using System;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Xrm.Tooling.Connector;

namespace Test_Connector.Utilities
{
    class D365Connector
    {
        private static string clientId;
        private static string clientSecret;
        private static string authority;
        private static string crmUrl;
        static D365Connector()
        {
            LoadConfiguration();
        }
        private static void LoadConfiguration()
        {
                var json = File.ReadAllText("appsettings.json");
                dynamic config = JsonConvert.DeserializeObject(json);
                clientId = config.D365.ClientId;
                clientSecret = config.D365.ClientSecret;
                authority = config.D365.Authority;
                crmUrl = config.D365.CrmUrl;  
        }
        public static CrmServiceClient GetServiceClient()
        {
            return new CrmServiceClient($"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};Authority={authority};RequireNewInstance=True");
        }
    }
}