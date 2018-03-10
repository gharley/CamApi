using System;
using System.Collections.Generic;
using System.Threading;

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

        private void FavoritesTest()
        {
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

        private void CaptureCancelPostFill(CamDictionary allowedSettings)
        {
            // Example showing full pre-trigger buffer fill, trigger, cancel while post-trigger buffer is filling
            RunCamera(allowedSettings, "Fill pre-trigger buffer, trigger, cancel while post-trigger buffer is filling");

            api.WaitForTransition("Waiting for pre-trigger buffer to fill", CAMERA_STATE.RUNNING, 10);
            api.ExpectState(CAMERA_STATE.RUNNING_PRETRIGGER_FULL);

            TriggerCamera("Camera triggered, starting to fill post-trigger buffer");

            Console.WriteLine("    Canceling trigger while post-trigger buffer is filling");
            Console.WriteLine($"        {api.GetStatusString()}");

            api.Cancel();
            api.WaitForTransition("Waiting for cancel to be processed", CAMERA_STATE.TRIGGERED, 10);

            api.ExpectRunningState();
        }

        private void CaptureSaveStop(CamDictionary allowedSettings)
        {
            // Example showing partial pre-trigger buffer fill, trigger, save video, then truncate save leaving playable video file
            RunCamera(allowedSettings, "Partial pre-trigger buffer fill, trigger, start video save, then truncate save leaving playable video file");

            api.WaitForTransition("    Letting pre-trigger buffer partially fill", CAMERA_STATE.RUNNING, 10);
            Thread.Sleep(4000);
            Console.WriteLine($"        {api.GetStatusString()}");

            TriggerCamera("Camera triggered, starting to fill post-trigger buffer");

            Console.WriteLine($"    Percentage pre-trigger buffer filled before trigger: {api.GetPretriggerFillLevel()}");

            api.WaitForTransition("Waiting for post-trigger buffer to fill", CAMERA_STATE.TRIGGERED, 10);
            api.ExpectState(CAMERA_STATE.SAVING);

            Thread.Sleep(1000);
            Console.WriteLine($"    Truncating save before complete, video file still playable");
            Console.WriteLine($"        {api.GetStatusString()}");

            api.SaveStop();
            api.WaitForTransition("Waiting for save to finish", CAMERA_STATE.SAVING, 10);
            api.WaitForTransition("Waiting for post-trigger buffer to fill", CAMERA_STATE.SAVE_TRUNCATING, 10);

            api.ExpectRunningState();
        }

        private void CaptureVideo(CamDictionary allowedSettings, string baseFilename = null)
        {
            // Example showing full pre-trigger buffer fill, trigger, save video capture process
            RunCamera(allowedSettings, "Partial pre-trigger buffer fill, trigger, save video capture process");

            api.WaitForTransition("    Waiting for pre-trigger buffer to fill", CAMERA_STATE.RUNNING, 10);
            api.ExpectState(CAMERA_STATE.RUNNING_PRETRIGGER_FULL);

            TriggerCamera("Camera triggered, filling post-trigger buffer");

            Console.WriteLine($"    Percentage pre-trigger buffer filled before trigger: {api.GetPretriggerFillLevel()}");

            api.WaitForTransition("Waiting for post-trigger buffer to fill", CAMERA_STATE.TRIGGERED, 10);
            api.ExpectState(CAMERA_STATE.SAVING);
            api.WaitForTransition("Waiting for save to complete", CAMERA_STATE.SAVING, 30);

            api.ExpectRunningState();
        }

        private void MultishotCaptureTests(bool TestDiscardingUnsavedVideos = false, bool TestCancellingPostTriggerFill = false)
        {
            var multishots = 3;
            var discardAfter = 0;
            var cancelPostTriggerFillAfter = 0;
            var requestedSettings = new CamDictionary(){
                {"requested_iso",null},
                {"requested_exposure",1/500.0},
                {"requested_frame_rate",60},
                {"requested_horizontal",640},
                {"requested_vertical",480},
                {"requested_subsample",1},
                {"requested_duration",10},
                {"requested_pretrigger",50},
                {"requested_multishot_count",null},
            };

            var allowedSettings = api.ConfigureCamera(requestedSettings);
            var multiShotCount = allowedSettings["multishot_count"];

            if (TestDiscardingUnsavedVideos)
            {
                discardAfter = 1;
                Console.WriteLine($"Multishot capture with discard (Max multishot captures: {multiShotCount}): fill pre-trigger buffer, trigger process, discard after {discardAfter} video saved)");
            }
            else if (TestCancellingPostTriggerFill)
            {
                cancelPostTriggerFillAfter = 2;
                Console.WriteLine($"Multishot capture with cancel post trigger fill (Max multishot captures: {multiShotCount}): fill pre-trigger buffer, trigger process, cancel after {cancelPostTriggerFillAfter} videos captured");
            }
            else
                Console.WriteLine($"Multishot capture (Max multishot captures: {multiShotCount}): fill pre-trigger buffer, trigger process");

            RunCamera(allowedSettings, "");

            for (int x = 0; x < multishots; x++)
            {
                var status = api.GetCamStatus();
                var activeBuffer = (long)status["active_buffer"];

                Console.WriteLine($"    Multishot buffer in use: {activeBuffer}");
                MultishotCaptureVideo(CAMERA_STATE.RUNNING, activeBuffer == cancelPostTriggerFillAfter);
            }

            if (TestCancellingPostTriggerFill) multishots = 1;
            MultiShotSaveVideos(multishots, discardAfter, 30);
        }

        private void MultishotCaptureVideo(CAMERA_STATE PostCaptureExpectedState, bool CancelDuringPostTriggerFill = false, string baseFilename = null)
        {
            // Example showing full pre-trigger buffer fill, trigger, save video capture process
            api.ExpectRunningState();
            api.WaitForTransition("Waiting for pre-trigger buffer to fill", CAMERA_STATE.RUNNING, 10);
            api.ExpectState(CAMERA_STATE.RUNNING_PRETRIGGER_FULL);

            TriggerCamera("Camera triggered, filling post-trigger buffer", baseFilename);
            Console.WriteLine($"    Percentage pre-trigger buffer filled before trigger: {api.GetPretriggerFillLevel()}");

            if (CancelDuringPostTriggerFill)
            {
                var status = api.Cancel();

                Console.WriteLine($"    Video captured canceled during post trigger fill: {status}");
                Thread.Sleep(1000);
                api.ExpectRunningState();
                return;
            }

            api.WaitForTransition("Waiting for post-trigger buffer to fill", CAMERA_STATE.TRIGGERED, 10);
            api.ExpectState(PostCaptureExpectedState);
        }

        private void MultiShotSaveVideos(long expectedVideoCount, int discardAfterVideo, int saveWait)
        {
            // param discard_after_video: set to zero to save all captured multishot videos or to the number of videos to save
            if (discardAfterVideo > 0)
                Console.WriteLine($"    Saving filled multishot buffers, discarding videos after video {discardAfterVideo} is saved");
            else
                Console.WriteLine("    Saving filled multishot buffers");

            api.ExpectState(CAMERA_STATE.RUNNING);

            api.Save();
            Thread.Sleep(1000);

            for (var x = 0; x < expectedVideoCount; x++)
            {
                var expectedBuffer = x + 1;
                var maxWait = saveWait;

                api.ExpectState(CAMERA_STATE.SAVING);

                var status = api.GetCamStatus();
                var capturedBuffers = (long)status["captured_buffers"];

                Console.WriteLine($"        Captured buffers: {capturedBuffers}");

                if (capturedBuffers != expectedVideoCount)
                {
                    Console.WriteLine($"        Error: captured buffer count doesn't match expected count: {capturedBuffers} != {expectedVideoCount}");
                    Environment.Exit(1);
                }

                while (expectedBuffer <= expectedVideoCount && maxWait > 0)
                {
                    // camera stays in the save state the entire time the set of captured videos are saved
                    // monitor progress by using the active buffer and save complete level
                    status = api.GetCamStatus();
                    var state = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), status["state"]);

                    if (state != CAMERA_STATE.SAVING)
                    {
                        Console.WriteLine($"        Current state: {api.GetTextState(state)}");
                        break;
                    }

                    var activeBuffer = (long)status["active_buffer"];

                    Console.WriteLine($"        Saving buffer {activeBuffer}/{expectedVideoCount} progress {status["level"]} ({maxWait})");
                    if (expectedBuffer + 1 == activeBuffer) break;

                    if (discardAfterVideo > 0 && discardAfterVideo + 1 == activeBuffer)
                    {
                        Console.WriteLine("    Discarding filled multishot buffers");
                        Thread.Sleep(2000);
                        api.SaveStop(true);

                        api.WaitForTransition("Waiting for truncation to finish", CAMERA_STATE.SAVE_TRUNCATING, 5);
                        api.ExpectRunningState();
                        return;
                    }

                    Thread.Sleep(1000);
                    maxWait--;
                }

                api.ExpectRunningState();
            }
        }

        private void RunCamera(CamDictionary allowedSettings, string message)
        {
            api.ExpectRunningState();

            if (!string.IsNullOrEmpty(message)) Console.WriteLine(message);
            Console.WriteLine("    Calibrating camera using allowed settings");

            CAMAPI_STATUS status = api.Run(allowedSettings);

            if (status == CAMAPI_STATUS.OKAY) Console.WriteLine("    Run started");
            else
            {
                Console.WriteLine("    Error: Run() returned {status}");
                Environment.Exit(1);
            }

            api.ExpectState(CAMERA_STATE.RUNNING);
        }

        private void TriggerCamera(string message, string baseFilename = null)
        {
            Console.WriteLine("    Triggering camera");

            var status = api.Trigger(baseFilename);

            if (status == CAMAPI_STATUS.OKAY) Console.WriteLine($"    {message}");
            else
            {
                Console.WriteLine("    Error: Trigger() returned {status}");
                Environment.Exit(1);
            }

            Thread.Sleep(1000); // takes camera time to process the trigger request
            api.ExpectState(CAMERA_STATE.TRIGGERED);
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

                FavoritesTest();

                CaptureCancelPostFill(settings);
                CaptureSaveStop(settings);

                CaptureVideo(settings);
                Console.WriteLine($"Last saved file: {api.GetLastSavedFilename()}");

                requestedSettings["duration"] = 1;
                settings = api.ConfigureCamera(requestedSettings);

                CaptureVideo(settings, "/tmp/hcamapi_tmp_test");
                Console.WriteLine($"    Last saved file - should be '/tmp/hcamapi_tmp_test': {api.GetLastSavedFilename()}");

                CaptureVideo(settings, "hcamapi_test");
                Console.WriteLine($"    Last saved file - should be 'hcamapi_test': {api.GetLastSavedFilename()}");

                MultishotCaptureTests();
                MultishotCaptureTests(true);
                MultishotCaptureTests(TestCancellingPostTriggerFill: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}