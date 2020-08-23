using Microsoft.Win32;
using System;

namespace SolarUninstallHelper
{
    class Program
    {

        private const string startupRegistryPath = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        //private const string startupRegistryPath = @"Environment";

        static void Main(string[] args)
        {
            Console.WriteLine(CheckForStartupEntry() ? "Startup entry found. Removing..." : "No startup entry found. Exiting...");
            RemoveStartupEntryIfNecessary();
        }

        static void RemoveStartupEntryIfNecessary()
        {
            if (CheckForStartupEntry()) RemoveStartupEntry();
        }

        static void RemoveStartupEntry()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey subKey = registryKey.CreateSubKey(startupRegistryPath);
                subKey.DeleteValue("Solar");
                Console.WriteLine("Entry successfully removed. Exiting...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error deleting key!");
                Console.WriteLine(e);
            }
        }

        static bool CheckForStartupEntry()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey subKey = registryKey.CreateSubKey(startupRegistryPath);
                string result = (string)subKey.GetValue("Solar");
                if (result != null) return true;
                else return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading key!");
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
