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

        public void FavoritesTest(){
            api.DeleteAllFavorites();
            Console.WriteLine($"Number of favorite IDs (should be 0): {api.GetFavoriteIds().Count}");

            var settings = api.GetCurrentSettings();

            settings["id"] = 5;
            settings["duration"] = 2;
            settings["notes"] = "Favorite settings stored in ID slot 5";

            api.SaveFavorite(settings);
            Console.WriteLine("Saved settings in ID slot 5");

            settings = api.GetCurrentSettings();

            settings["id"] = 3;
            settings["duration"] = 4;
            settings["notes"] = "Favorite settings stored in ID slot 3";

            api.SaveFavorite(settings);
            Console.WriteLine("Saved settings in ID slot 3");

            Console.WriteLine($"Saved favorite ID list: {string.Join(", ", api.GetFavoriteIds())}");

            settings = api.GetFavorite("5");
            var allowedSettings = api.ConfigureCamera(settings);

            api.Run(allowedSettings);
            Console.WriteLine("Retreived favorite settings 5 and configured the camera using those settings");

            api.DeleteFavorite("3");
            Console.WriteLine("Deleted previously save favorite in ID slot 3");
            Console.WriteLine($"Saved favorite ID list: {string.Join(", ", api.GetFavoriteIds())}");
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

                settings = api.GetCurrentSettings();

                Console.WriteLine("Current requested camera settings:");
                api.PrintSettings(settings, "requested_", "    ");

                Console.WriteLine("Current allowed camera settings:");
                api.PrintSettings(settings, "", "    ");

                var requestedSettings = new Dictionary<string, object>(){
                    {"requested_iso", null},
                    {"requested_exposure", 1/500.0},
                    {"requested_frame_rate", 60},
                    {"requested_horizontal", 640},
                    {"requested_vertical", 480},
                    {"requested_subsample", 1},
                    {"requested_duration", 10},
                    {"requested_pretrigger", 50},
                    {"requested_multishot_count", 1}
                };

                settings = api.ConfigureCamera(requestedSettings);

                Console.WriteLine("Requested camera settings:");
                api.PrintSettings(settings, "requested_", "    ");

                Console.WriteLine("Allowed camera settings:");
                api.PrintSettings(settings, "", "    ");

                FavoritesTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}