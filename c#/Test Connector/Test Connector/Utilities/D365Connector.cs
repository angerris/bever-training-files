using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Test_Connector.Model;

namespace Test_Connector.Utilities
{
     class D365Connector
    {
       private static string clientId = "3840b01c-7b1c-4f37-9bdd-79a2a3722c86";
       private static string clientSecret = "n458Q~Rb.tU0TsJMok.fALBIAJrHbS0ihrkAocxS";
       private static string authority = "https://login.microsoftonline.com/91b242cb-658d-4278-bd71-2c38fde7444c";
       private static string crmUrl = "https://org0f359662.crm4.dynamics.com/";
       private static string conString = $"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};Authority={authority};RequireNewInstance=True";
       private CrmServiceClient crmService = new CrmServiceClient(conString);

       
        public PriceList getPriceListByName(string PriceListName )
        {
            PriceList pricelistObj = null;
            QueryExpression priceListsQuery = new QueryExpression{
                EntityName= "cr8c9_price_list",
                ColumnSet = new ColumnSet("cr8c9_name", "transactioncurrencyid"),
                Criteria =
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression("cr8c9_name", ConditionOperator.Equal,PriceListName )
                    }
                }
            };
           EntityCollection priceLists =  crmService.RetrieveMultiple(priceListsQuery);
            if (priceLists.Entities.Count > 0) { 
                Entity priceList = priceLists.Entities[0];
                pricelistObj = new PriceList();
                pricelistObj.priceListId = priceList.Id;
                pricelistObj.priceListName = priceList.GetAttributeValue<string>("cr8c9_name");
                EntityReference transactionCurrency = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");
                if (transactionCurrency != null) { 
                  pricelistObj.currencyId = transactionCurrency.Id;
                }
            }
            return pricelistObj;
        }

        public List<Product> getAllProducts()
        {
            List<Product> productObjs = null;
            QueryExpression productsQuery = new QueryExpression
            {
                EntityName = "cr8c9_product",
                ColumnSet = new ColumnSet("cr8c9_name")
            };
            EntityCollection products = crmService.RetrieveMultiple(productsQuery);
            if (products.Entities.Count > 0)
            {
                productObjs = new List<Product>();
                foreach (Entity product in products.Entities)
                {
                    Product productObj = new Product();
                    productObj.productId = product.Id;
                    productObj.productName = product.GetAttributeValue<string>("cr8c9_name");
                    productObjs.Add(productObj);
                }
            }
            return productObjs;
        }

        public void createPriceListItem(PriceList priceList, Product product)
        {
            Entity priceListItem = new Entity("cr8c9_price_list_item");

            priceListItem["cr8c9_name"] = product.productName;
            priceListItem["cr8c9_fk_price_list"] = new EntityReference("cr8c9_price_list", priceList.priceListId);
            priceListItem["cr8c9_fk_product"] = new EntityReference("cr8c9_product", product.productId);
            priceListItem["transactioncurrencyid"] = new EntityReference("transactioncurrency", priceList.currencyId);
            priceListItem["cr8c9_mon_price"] = new Money(1);

            crmService.Create(priceListItem);
        }

    }
}
