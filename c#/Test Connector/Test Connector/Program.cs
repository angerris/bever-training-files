using System.Threading;
using System;
using Test_Connector.ConsoleApps;

namespace Test_D365_Connector
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Main Menu ---");
                Console.WriteLine("1. Initialize Price List");
                Console.WriteLine("2. Manage Inventory");
                Console.WriteLine("3. Exit");
                Console.Write("Please select an option: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        PriceListManager.InitializePricelist(args);
                        break;
                    case "2":             
                        InventoryManager.ManageInventory(args);
                        break;
                    case "3":
                        Console.WriteLine("Exiting the application...");
                        Thread.Sleep(500);
                        return;
                    default:
                        Console.WriteLine("Invalid option, please try again.");
                        break;
                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        
        }
    }
}
