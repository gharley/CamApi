using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using CamApi;

namespace CamApiExample
{
    class Program
    {
        // Valid command line args
        private static IReadOnlyDictionary<string, string> validArgs = new Dictionary<string, string>(){
            {"Address", "10.11.12.13"},
            {"Debug", "0"},
            {"CaptureTest", "0"},
            {"FavoritesTest", "0"},
            {"MultiCaptureTest", "0"},
            {"Help", "0"}
        };

        private static IConfiguration config { get; set; }

        private static Dictionary<string, string> GetSwitchMappings()
        {
            return validArgs.Select(item =>
                 new KeyValuePair<string, string>(
                     "-" + item.Key.Substring(0, 1),
                     item.Key))
                     .ToDictionary(
                         item => item.Key, item => item.Value);
        }

        private static Boolean GetConfiguration(string[] args)
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(validArgs).AddCommandLine(args, GetSwitchMappings());

            try
            {
                config = builder.Build();
                return true;
            }
            catch (Exception)
            {
                Usage();
                return false;
            }
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: CamApiExample [options]");
            Console.WriteLine("  options:");
            Console.WriteLine("    -a --Address           Host name or IP # of camera (default 10.11.12.13");
            Console.WriteLine("    -f --FavoritesTest     1 to run favorites tests (deletes all saved favorites)");
            Console.WriteLine("    -c --CaptureTest       1 to run capture tests (time consuming, writes to storage)");
            Console.WriteLine("    -m --MultiCaptureTest  1 to run multi-capture tests (time consuming, writes to storage)");
            Console.WriteLine("    -h --Help              1 to display this message");
            Console.WriteLine("");
        }

        static void Main(string[] args)
        {
            if (GetConfiguration(args))
            {
                try
                {
                    if (config["Help"] == "1")
                    {
                        Usage();
                        return;
                    }

                    var camExample = new CamApiExample(config);

                    camExample.TestCameraFunctionality();
                }
                catch (Exception) { Usage(); return; }
            }
        }
    }
}
