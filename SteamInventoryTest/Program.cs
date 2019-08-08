using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace SteamInventoryTest
{
    class Program
    {
        static bool shouldExit;

        static void Main(string[] args)
        {
            if (!SteamAPI.Init())
                ExitWithMessage(-1, "Could not init Steam API.");

            try
            {
                InventoryDemo demo = new InventoryDemo();
                demo.RunDemo();
                while (!shouldExit)
                {
                    SteamAPI.RunCallbacks();
                    //System.Threading.Thread.Sleep(16);
                }
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
            }
            catch (Exception ex)
            {
                ExitWithMessage(-2, "Something failed: " + ex.Message);
            }
            finally
            {
                SteamAPI.Shutdown();
            }
        }

        static void ExitWithMessage(int exitCode, string message)
        {
            Console.WriteLine(message);
            Console.ReadKey(true);
            Environment.Exit(exitCode);
        }

        public static void ExitApp()
        {
            shouldExit = true;
        }
    }
}
