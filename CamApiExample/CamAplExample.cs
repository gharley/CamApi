using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;

using CamApi;
using CamApiExtensions;

namespace CamApiExample
{
    public class CamApiExample
    {
        private CamApiLib api;
        private bool DoCaptureTest;
        private bool DoFavoritesTest;
        private bool DoMultiCaptureTest;

        public CamApiExample(IConfiguration configuration)
        {
            DoCaptureTest = configuration["CaptureTest"] == "1";
            DoFavoritesTest = configuration["FavoritesTest"] == "1";
            DoMultiCaptureTest = configuration["MultiCaptureTest"] == "1";

            api = new CamApiLib(configuration["Address"], configuration["Debug"] == "1");
        }

        public void TestCameraFunctionality()
        {
            try
            {
                var camStatus = api.GetCamStatus();

                Console.WriteLine($"Camera state {camStatus["state"]}, level {camStatus["level"]}, flags {camStatus["flags"]}");

                Console.WriteLine($"\nCamera extended status - state {camStatus["state"]}, level {camStatus["level"]}, flags {camStatus["flags"]}, IS temp (C) {camStatus["is_temp"]}, FPGA temp (C) {camStatus["fpga_temp"]}");

                Console.WriteLine($"\nStatus string: {api.GetStatusString()}");

                Console.WriteLine($"\nDirectory path to active storage device: {api.GetStorageDir()}");

                var storageInfo = api.GetStorageInfo();
                Console.WriteLine($"\nStorage information: {storageInfo["available_space"]} / " +
                    $"{storageInfo["storage_size"]} bytes, mount point: {storageInfo["mount_point"]}");

                Console.WriteLine("\nCamera information:");
                Console.Write(api.GetInfoString("    "));

                var settings = api.GetSavedSettings();

                Console.WriteLine("\nSaved camera settings:");
                api.PrintSettings(settings, "requested_", "    ");

                settings = api.GetCurrentSettings();

                Console.WriteLine("\nCurrent requested camera settings:");
                api.PrintSettings(settings, "requested_", "    ");

                Console.WriteLine("\nCurrent allowed camera settings:");
                api.PrintSettings(settings, "", "    ");

                var requestedSettings = new CamDictionary(){
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

                Console.WriteLine("\nRequested camera settings:");
                api.PrintSettings(settings, "requested_", "    ");

                Console.WriteLine("\nAllowed camera settings:");
                api.PrintSettings(settings, "", "    ");

                if (DoFavoritesTest)
                {
                    var favoritesTests = new FavoritesTests(api);
                    favoritesTests.Run();
                }

                if (DoCaptureTest)
                {
                    var captureTests = new CaptureTests(api);

                    captureTests.RunCaptureCancelPostFill(settings);
                    captureTests.RunCaptureSaveStop(settings);
                    captureTests.RunCaptureVideo(settings, "Last saved file: {0}");

                    requestedSettings["duration"] = 1;
                    settings = api.ConfigureCamera(requestedSettings);

                    captureTests.RunCaptureVideo(settings, "Last saved file - should be '/tmp/hcamapi_tmp_test': {0}", "/tmp/hcamapi_tmp_test");
                    captureTests.RunCaptureVideo(settings, "Last saved file - should be 'hcamapi_test': {0}", "hcamapi_test");
                }

                if (DoMultiCaptureTest)
                {
                    var multishotCaptureTests = new MultiCaptureTests(api);

                    multishotCaptureTests.Run();
                    multishotCaptureTests.Run(true);
                    multishotCaptureTests.Run(TestCancellingPostTriggerFill: true);
                }

                if (DoCaptureTest || DoMultiCaptureTest)
                {
                    Console.WriteLine("\nListing files in active storage video directory");

                    var fileList = api.FetchRemoteDirectoryListing();

                    foreach (var file in fileList)
                    {
                        Console.WriteLine($"    {file}");
                    }

                    string lastVideoFilename = api.GetLastSavedFilename();

                    api.DisplayRemoteFile($"{lastVideoFilename.Substring(1, lastVideoFilename.Length - 6)}.txt");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}