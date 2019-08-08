using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace SteamInventoryTest
{
    class InventoryDemo
    {
        static readonly uint STRING_BUFFER_SIZE = 4096;

        SteamInventoryResult_t currentResult = SteamInventoryResult_t.Invalid;
        Action nextAction;
        SteamItemDetails_t[] currentDetails;

        // Make sure you use callbacks
        Callback<SteamInventoryResultReady_t> resultReadyCallback;
        Callback<SteamInventoryFullUpdate_t> fullUpdateCallback;
        Callback<SteamInventoryDefinitionUpdate_t> definitionUpdateCallback;
        
        public InventoryDemo()
        {
            resultReadyCallback = Callback<SteamInventoryResultReady_t>.Create(ResultReadyCallbackHandler);
            fullUpdateCallback = Callback<SteamInventoryFullUpdate_t>.Create(FullUpdateCallbackHandler);
            definitionUpdateCallback = Callback<SteamInventoryDefinitionUpdate_t>.Create(DefinitionUpdateCallbackHandler);
        }

        void EnsureNoOngoingOperation()
        {
            if (currentResult != SteamInventoryResult_t.Invalid)
                throw new InvalidOperationException("There is already an ongoing operation.");
        }

        void EnsureStatusOK()
        {
            if (currentResult == SteamInventoryResult_t.Invalid)
                throw new InvalidOperationException("There is no current operation ongoing.");
            if (SteamInventory.GetResultStatus(currentResult) != EResult.k_EResultOK)
                throw new InvalidOperationException($"Result status is not OK. It is {SteamInventory.GetResultStatus(currentResult)}.");
        }

        public void RunDemo()
        {
            // Demo 1: get all items
            EnsureNoOngoingOperation();
            SteamInventoryResult_t result;
            if (!SteamInventory.GetAllItems(out result))
                throw new InvalidOperationException("Failed to get all items. You're probably running this as a server.");
            currentResult = result;
            nextAction = ListItems;
        }

        void ListItems()
        {
            EnsureStatusOK();
            Console.WriteLine("Items obtained.");

            // Demo 2: fetching items from result
            // Get the number of items available first, by setting the items array argument to null
            uint itemCount = 0;
            if (!SteamInventory.GetResultItems(currentResult, null, ref itemCount))
                throw new Exception("Failed to get item count.");

            // Now get the items
            currentDetails = new SteamItemDetails_t[itemCount];
            if (!SteamInventory.GetResultItems(currentResult, currentDetails, ref itemCount))
                throw new Exception("Failed to get items.");

            // We're going to fetch items individually afterwards, so destroy result
            SteamInventory.DestroyResult(currentResult);
            currentResult = SteamInventoryResult_t.Invalid;

            nextAction = null;
            InteractiveGetDetails();
        }

        void PrintItems()
        {
            Console.WriteLine("Index\tInstance ID\tDefinition number\tQuantity\tFlags");
            for (int i = 0; i < currentDetails.Length; ++i)
            {
                var item = currentDetails[i];
                Console.WriteLine($"{i}\t{item.m_itemId}\t{item.m_iDefinition}\t{item.m_unQuantity}\t{(ESteamItemFlags)item.m_unFlags}");
            }
        }

        void InteractiveGetDetails()
        {
            EnsureNoOngoingOperation();
            if (currentDetails == null)
                throw new InvalidOperationException("Items have not been fetched.");

            while (true)
            {
                Console.WriteLine();
                PrintItems();
                Console.WriteLine("Enter index of item to get details of. Enter not a number to exit.");
                Console.Write("Index: ");
                string input = Console.ReadLine();
                int selectedIndex;
                if (!int.TryParse(input, out selectedIndex))
                {
                    Program.ExitApp();
                    break;
                }

                if (selectedIndex < 0 || selectedIndex >= currentDetails.Length)
                {
                    Console.WriteLine("Index out of range, try again.");
                    continue;
                }

                // Demo 3: get items by ID
                SteamItemInstanceID_t[] ids = new[] { currentDetails[selectedIndex].m_itemId };
                SteamInventoryResult_t result;
                if (!SteamInventory.GetItemsByID(out result, ids, (uint)ids.Length))
                    throw new InvalidOperationException("Failed to get items by ID. You're probably running this as a server.");
                currentResult = result;
                nextAction = PrintItemDetail;
                break;
            }
        }

        void PrintItemDetail()
        {
            // Demo 4: print item details
            // Similar to getting all results, use GetResultItems();
            EnsureStatusOK();
            Console.WriteLine("Items by ID obtained.");

            // Get the number of items available first, by setting the items array argument to null
            uint itemCount = 0;
            if (!SteamInventory.GetResultItems(currentResult, null, ref itemCount))
                throw new Exception("Failed to get item count.");

            // Now get the items
            SteamItemDetails_t[] details = new SteamItemDetails_t[itemCount];
            if (!SteamInventory.GetResultItems(currentResult, details, ref itemCount))
                throw new Exception("Failed to get items.");

            if (details.Length != 1)
                throw new Exception("We don't have just one item?");

            SteamItemDetails_t item = details[0];
            Console.WriteLine();

            // Print basic properties
            Console.WriteLine($"Instance ID: {item.m_itemId}");
            Console.WriteLine($"Definition number: {item.m_iDefinition}");
            Console.WriteLine($"Quantity: {item.m_unQuantity}");
            Console.WriteLine($"Flags: {(ESteamItemFlags)item.m_unFlags}");

            // Print item properties
            string[] itemKeys = GetResultItemProperty(0, null).Split(',');
            foreach (var key in itemKeys)
            {
                string value = GetResultItemProperty(0, key);
                if (value == null)
                {
                    Console.WriteLine($"Could not get value for \"{key}\".");
                }
                else
                {
                    Console.WriteLine($"{key}: {value}");
                }
            }

            Console.WriteLine("--------------------------------------------------------------------------------");

            // Print item definition properties
            Console.WriteLine($"Definition number: {item.m_iDefinition}");
            string[] itemDefKeys = GetItemDefinitionProperty(item.m_iDefinition, null).Split(',');
            foreach (var key in itemDefKeys)
            {
                string value = GetItemDefinitionProperty(item.m_iDefinition, key);
                if (value == null)
                {
                    Console.WriteLine($"Could not get value for \"{key}\".");
                }
                else
                {
                    Console.WriteLine($"{key}: {value}");
                }
            }

            // Destroy result
            SteamInventory.DestroyResult(currentResult);
            currentResult = SteamInventoryResult_t.Invalid;

            nextAction = null;
            InteractiveGetDetails();
        }

        string GetResultItemProperty(int index, string key)
        {
            string value;
            uint valLength = STRING_BUFFER_SIZE;
            if (!SteamInventory.GetResultItemProperty(currentResult, (uint)index, key, out value, ref valLength))
                return null;
            return value;
        }

        string GetItemDefinitionProperty(SteamItemDef_t definition, string key)
        {
            string value;
            uint valLength = STRING_BUFFER_SIZE;
            if (!SteamInventory.GetItemDefinitionProperty(definition, key, out value, ref valLength))
                return null;
            return value;
        }

        void EndDemo()
        {
            currentResult = SteamInventoryResult_t.Invalid;
            Program.ExitApp();
        }

        void ResultReadyCallbackHandler(SteamInventoryResultReady_t param)
        {
            Console.WriteLine($"Received {nameof(SteamInventoryResultReady_t)} callback.");
            if (param.m_handle == currentResult)
            {
                if (nextAction != null) nextAction();
            }
            else
            {
                Console.WriteLine("Handle in callback does not match current handle!");
            }
        }

        void FullUpdateCallbackHandler(SteamInventoryFullUpdate_t param)
        {
            Console.WriteLine($"Received {nameof(SteamInventoryFullUpdate_t)} callback.");
        }

        void DefinitionUpdateCallbackHandler(SteamInventoryDefinitionUpdate_t param)
        {
            Console.WriteLine($"Received {nameof(SteamInventoryDefinitionUpdate_t)} callback.");
        }
    }
}
