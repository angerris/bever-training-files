using System;
using System.Collections.Generic;
using System.Threading; 
using Test_Connector.Utilities;
using Test_Connector.Model;

namespace Test_D365_Connector
{
    class Program
    {
        static void Main(string[] args)
        {
            D365Connector d365connector = new D365Connector();
            Console.WriteLine("Successfully connected to D365.");
            Console.WriteLine("Enter Pricelist Name:");
            string priceListName = Console.ReadLine();
            PriceList priceList = d365connector.getPriceListByName(priceListName);
            List<Product> products = d365connector.getAllProducts();
            foreach (Product product in products)
            {
                d365connector.createPriceListItem(priceList, product);
            }
            Console.WriteLine("Price List Initialized successfully!");
            Thread.Sleep(3000);
        }
    }
}
