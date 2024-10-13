using System;
using System.IO;
using System.Net;
using System.Activities;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;

namespace Currency_Management
{
    public class UpdateUSDExchangeRate : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            decimal usdPerAmd = GetExchangeRateFromCentralBank();

            if (usdPerAmd > 0)
            {
                QueryExpression query = new QueryExpression("transactioncurrency")
                {
                    ColumnSet = new ColumnSet("transactioncurrencyid", "exchangerate"),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("isocurrencycode", ConditionOperator.Equal, "USD")
                        }
                    }
                };

                EntityCollection currencies = service.RetrieveMultiple(query);

                if (currencies.Entities.Count > 0)
                {
                    Entity usdCurrency = currencies.Entities[0];
                    usdCurrency["exchangerate"] = usdPerAmd;
                    service.Update(usdCurrency);
                }
              
            }
          
        }

        private decimal GetExchangeRateFromCentralBank()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.cba.am/exchangerates.asmx?op=ExchangeRatesByDate");
            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";

            string soapXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                  <soap:Body>
                    <ExchangeRatesByDate xmlns=""http://www.cba.am/"">
                      <date>" + DateTime.UtcNow.ToString("yyyy-MM-dd") + @"</date>
                    </ExchangeRatesByDate>
                  </soap:Body>
                </soap:Envelope>";

            XmlDocument xmlReq = new XmlDocument();
            xmlReq.LoadXml(soapXml);

            using (Stream stream = webRequest.GetRequestStream())
            {
                xmlReq.Save(stream);
            }

            WebResponse res = webRequest.GetResponse();
            XmlDocument xmlRes = new XmlDocument();
            xmlRes.Load(res.GetResponseStream());

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlRes.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("ns", "http://www.cba.am/");

            XmlNodeList exchangeRateNodes = xmlRes.SelectNodes("//ns:ExchangeRate", nsmgr);
            decimal usdPerAmd = 0;

            foreach (XmlNode node in exchangeRateNodes)
            {
                string isoCode = node.SelectSingleNode("ns:ISO", nsmgr)?.InnerText;
                if (isoCode == "USD")
                {
                    string exchangeRateStr = node.SelectSingleNode("ns:Rate", nsmgr)?.InnerText;
                    if (decimal.TryParse(exchangeRateStr, out decimal exchangeRate))
                    {
                        usdPerAmd = 1 / exchangeRate;
                        break;
                    }
                }
            }

            return usdPerAmd;
        }
    }
}
