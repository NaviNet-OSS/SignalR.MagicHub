using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SignalR.MagicHub.Performance;

namespace SignalR.MagicHub.PerformanceCounterInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "MagicHub Performance Counter Installer Version: {0}", Assembly.GetExecutingAssembly().GetName().Version));

            try
            {
                if (args.Length > 0 && args[0] == "/u")
                {
                    Uninstall();
                }
                else
                {
                    Install();
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                Environment.Exit(1);
            }
        }

        private static void Install()
        {
            Uninstall();
            IList<string> counters;
            PrintInfo("Installing Performance Counters...");
            try
            {
                var counterCreationData = MagicHubPerformanceCounterManager.GetCounterPropertyInfo()
                .Select(p =>
                {
                    var attribute = MagicHubPerformanceCounterManager.GetPerformanceCounterAttribute(p);
                    return new CounterCreationData(attribute.Name, attribute.Description, attribute.CounterType);
                })
                .ToArray();

                var createDataCollection = new CounterCreationDataCollection(counterCreationData);
                
                var category = PerformanceCounterCategory.Create(MagicHubPerformanceCounterManager.CategoryName,
                    "MagicHub performance counters",
                    PerformanceCounterCategoryType.MultiInstance,
                    createDataCollection);

                counters = counterCreationData.Select(c => c.CounterName).ToList();
            }
            catch (UnauthorizedAccessException ex)
            {
                // Probably due to not running as admin, let's just stop here
                PrintWarning(String.Format(CultureInfo.CurrentCulture, ex.Message + "\r\nMake sure you are running this as administrator."));
                return;
            }

            foreach (var counter in counters)
            {
                PrintInfo("  " + counter);
            }


        }

        private static void Uninstall()
        {
            if (PerformanceCounterCategory.Exists("MagicHub"))
            {
                PerformanceCounterCategory.Delete("MagicHub");
            }

            PrintSuccess("MagicHub Performance Counters uninstalled!");
        }

        private static void PrintInfo(string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }
        }

        private static void PrintWarning(string info)
        {
            if (!String.IsNullOrWhiteSpace(info))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "Warning: " + info));
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        private static void PrintError(string error)
        {
            if (!String.IsNullOrWhiteSpace(error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "Error: " + error));
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        private static void PrintSuccess(string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine(message);
                Console.WriteLine();
                Console.ResetColor();
            }
        }
    }
}
