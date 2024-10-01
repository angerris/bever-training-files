using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Test_Connector.Model;
using Microsoft.Xrm.Tooling.Connector;
using Test_Connector.Utilities;

namespace Test_Connector.Services
{    
     class PriceListService
    {
        private static CrmServiceClient crmService = D365Connector.GetServiceClient();
        public PriceList getPriceListByName(string PriceListName)
        {
            PriceList pricelistObj = null;
            QueryExpression priceListsQuery = new QueryExpression
            {
                EntityName = "cr8c9_price_list",
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
            EntityCollection priceLists = crmService.RetrieveMultiple(priceListsQuery);
            if (priceLists.Entities.Count > 0)
            {
                Entity priceList = priceLists.Entities[0];
                pricelistObj = new PriceList();
                pricelistObj.priceListId = priceList.Id;
                pricelistObj.priceListName = priceList.GetAttributeValue<string>("cr8c9_name");
                EntityReference transactionCurrency = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");
                if (transactionCurrency != null)
                {
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
