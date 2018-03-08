using System;
using System.Collections.Generic;

using CamApi;

namespace CamApiCli
{
    public class CamApiCli
    {
        private CamApiLib api;

        public CamApiCli(string address)
        {
            api = new CamApiLib(address);
        }
        public void GetCamStatus()
        {
            var result = api.GetCamStatus();

            foreach (var kv in result)
            {
                Console.WriteLine(kv.Key + ": " + kv.Value);
            }
        }

        public void TestBasicFunctionality()
        {
            try
            {
                var camStatus = api.GetCamStatus();

                Console.WriteLine($"Camera state {camStatus["state"]}, level {camStatus["level"]}, flags {camStatus["flags"]}");

                Console.WriteLine($"Camera extended status - state {camStatus["state"]}, level {camStatus["level"]}, flags {camStatus["flags"]}, IS temp (C) {camStatus["is_temp"]}, FPGA temp (C) {camStatus["fpga_temp"]}");

                Console.WriteLine($"Status string: {api.GetStatusString()}");

                Console.WriteLine($"Directory path to active storage device: {api.GetStorageDir()}");

                var storageInfo = api.GetStorageInfo();
                Console.WriteLine($"Storage information: {storageInfo["available_space"]} / " + 
                    $"{storageInfo["storage_size"]} bytes, mount point: {storageInfo["mount_point"]}");

                Console.WriteLine("Camera information:");
                Console.Write(api.GetInfoString("    "));

                var settings = api.GetSavedSettins();
                Console.WriteLine("Saved camera settings:");
                api.PrintSettings(settings, "requested_", "    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}