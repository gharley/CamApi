using System;
using System.Threading;
using Newtonsoft.Json;

using CamApi;
using CamApiExampleExtentions;

namespace CamApiExample
{
    public class MultiCaptureTests
    {
        private CamApiLib api;

        public MultiCaptureTests(CamApiLib api){
            this.api = api;
        }

        private void MultishotCaptureVideo(CAMERA_STATE PostCaptureExpectedState, bool CancelDuringPostTriggerFill = false, string baseFilename = null)
        {
            // Example showing full pre-trigger buffer fill, trigger, save video capture process
            api.ExpectRunningState();
            api.WaitForTransition("Waiting for pre-trigger buffer to fill", CAMERA_STATE.RUNNING, 10);
            api.ExpectState(CAMERA_STATE.RUNNING_PRETRIGGER_FULL);

            this.TriggerCamera(api, "Camera triggered, filling post-trigger buffer", baseFilename);
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

            Save();
            Thread.Sleep(1000);

            for (var x = 0; x < expectedVideoCount; x++)
            {
                var expectedBuffer = x + 1;
                var maxWait = saveWait;

                api.ExpectState(CAMERA_STATE.SAVING);

                var camStatus = api.GetCamStatus();
                var capturedBuffers = (long)camStatus["captured_buffers"];

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
                    camStatus = api.GetCamStatus();
                    var state = (CAMERA_STATE)camStatus["state"];

                    if (state != CAMERA_STATE.SAVING)
                    {
                        Console.WriteLine($"        Current state: {api.GetTextState(state)}");
                        break;
                    }

                    var activeBuffer = (long)camStatus["active_buffer"];

                    Console.WriteLine($"        Saving buffer {activeBuffer}/{expectedVideoCount} progress {camStatus["level"]} ({maxWait})");
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
            }

            api.ExpectRunningState();
        }

        public void Run(bool TestDiscardingUnsavedVideos = false, bool TestCancellingPostTriggerFill = false)
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

            this.RunCamera(api, allowedSettings, "");

            for (int x = 0; x < multishots; x++)
            {
                var camStatus = api.GetCamStatus();
                var activeBuffer = (long)camStatus["active_buffer"];

                Console.WriteLine($"    Multishot buffer in use: {activeBuffer}");
                MultishotCaptureVideo(CAMERA_STATE.RUNNING, activeBuffer == cancelPostTriggerFillAfter);
            }

            if (TestCancellingPostTriggerFill) multishots = 1;
            MultiShotSaveVideos(multishots, discardAfter, 30);
        }

    public CAMAPI_STATUS Save()
    {
      // Saves videos when multishot capture is enabled and one or more multishot buffers contain unsaved videos.
      // :return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE
      string url = "/save";

      string jdata = api.FetchTarget(url);

      return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
    }
    }
}