using System;
using Test_Connector.Services;

namespace Test_Connector.ConsoleApps
{
    class InventoryManager
    {
        public static void ManageInventory(string[] args)
        {
            InventoryService service = new InventoryService();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Dynamics 365 Inventory Management");

                string inventoryName = GetInput("Enter Inventory Name: ");
                string productName = GetInput("Enter Product Name: ");
                int quantity = GetQuantityInput("Enter Quantity: ");
                string operationType = GetInput("Enter Type of Operation (In/Out): ");

                Guid inventoryId = service.GetInventoryIdByName(inventoryName);
                if (inventoryId == Guid.Empty)
                {
                    Console.WriteLine("Inventory not found.");
                    continue;
                }
                Guid productId = service.GetProductIdByName(productName);
                if (productId == Guid.Empty)
                {
                    Console.WriteLine("Product not found.");
                    continue;
                }
                PerformInventoryOperation(service, operationType, inventoryId, productId, quantity);

                if (!PromptForAnotherOperation("Do you want to perform another operation? (y/n): "))
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
        private static int GetQuantityInput(string message)
        {
            Console.Write(message);
            return int.Parse(Console.ReadLine());
        }
        private static void PerformInventoryOperation(InventoryService service, string operationType, Guid inventoryId, Guid productId, int quantity)
        {
            if (operationType.Equals("Out", StringComparison.OrdinalIgnoreCase))
            {
                service.HandleSubtraction(inventoryId, productId, quantity);
            }
            else if (operationType.Equals("In", StringComparison.OrdinalIgnoreCase))
            {
                service.HandleAddition(inventoryId, productId, quantity);
            }
            else
            {
                Console.WriteLine("Invalid operation type.");
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
