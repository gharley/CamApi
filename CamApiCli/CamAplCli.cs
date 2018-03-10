using System;
using System.Collections.Generic;
using System.Threading;

using CamApi;

namespace CamApiCli
{
    public class CamApiCli
    {
        private CamApiLib api;

        public CamApiCli(string address, bool debug = false)
        {
            api = new CamApiLib(address, debug);
        }

        public void TestCameraFunctionality()
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

                Console.WriteLine("Requested camera settings:");
                api.PrintSettings(settings, "requested_", "    ");

                Console.WriteLine("Allowed camera settings:");
                api.PrintSettings(settings, "", "    ");

                var favoritesTests = new FavoritesTests(api);
                favoritesTests.Run();

                var captureTests = new CaptureTests(api);

                captureTests.RunCaptureCancelPostFill(settings);
                captureTests.RunCaptureSaveStop(settings);
                captureTests.RunCaptureVideo(settings, "Last saved file: {0}");

                requestedSettings["duration"] = 1;
                settings = api.ConfigureCamera(requestedSettings);

                captureTests.RunCaptureVideo(settings, "Last saved file - should be '/tmp/hcamapi_tmp_test': {0}", "/tmp/hcamapi_tmp_test");
                captureTests.RunCaptureVideo(settings, "Last saved file - should be 'hcamapi_test': {0}", "hcamapi_test");

                var multishotCaptureTests = new MultiCaptureTests(api);

                multishotCaptureTests.Run();
                multishotCaptureTests.Run(true);
                multishotCaptureTests.Run(TestCancellingPostTriggerFill: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}