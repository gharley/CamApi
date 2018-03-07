using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using CamApi;

namespace CamApiCli
{
    class Program
    {
        // Valid command line args
        private static IReadOnlyDictionary<string, string> validArgs = new Dictionary<string, string>(){
            {"Address", "10.11.12.13"}
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

        private static Boolean GetConfiguration(string[] args){
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(validArgs).AddCommandLine(args, GetSwitchMappings());
            
            try{
                config = builder.Build();
                return true;
            }catch(Exception){
                Usage();
                return false;
            }
        }

        private static void Usage(){
            Console.WriteLine("Usage:");
        }

        static void Main(string[] args)
        {
            if( !GetConfiguration(args) ) return;

            var cli = new CamApiCli(config["Address"]);

        }
    }
}
