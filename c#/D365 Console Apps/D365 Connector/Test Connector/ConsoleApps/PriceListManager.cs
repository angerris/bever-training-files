using System;
using System.Collections.Generic;
using Test_Connector.Model;
using Test_Connector.Services;

namespace Test_Connector.ConsoleApps
{
    class PriceListManager
    {
        public static void InitializePricelist(string[] args)
        {
            PriceListService service = new PriceListService();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Dynamics 365 Pricelist Management");

                string priceListName = GetInput("Enter Pricelist Name: ");
                PriceList priceList = service.getPriceListByName(priceListName);
                List<Product> products = service.getAllProducts();

                InitializePriceListItems(service, priceList, products);

                Console.WriteLine("Pricelist Initialized successfully!");

                if (!PromptForAnotherOperation("Do you want to initialize another price list? (y/n): "))
                {
                    break; 
                }
            }
        }

        private static string GetInput(string message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }
        private static void InitializePriceListItems(PriceListService service, PriceList priceList, List<Product> products)
        {
            foreach (Product product in products)
            {
                service.createPriceListItem(priceList, product);
            }
        }
        private static bool PromptForAnotherOperation(string message)
        {
            Console.Write(message);
            string response = Console.ReadLine();
            return response.Equals("y", StringComparison.OrdinalIgnoreCase);
        }
    }
}
