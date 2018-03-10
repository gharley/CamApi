using System;
using System.Threading;

using CamApi;

namespace CamApiCli
{
    public class CaptureTests
    {
        private CamApiLib api;

        public CaptureTests(CamApiLib api)
        {
            this.api = api;
        }

        public void RunCaptureCancelPostFill(CamDictionary allowedSettings)
        {
            // Example showing full pre-trigger buffer fill, trigger, cancel while post-trigger buffer is filling
            this.RunCamera(api, allowedSettings, "Fill pre-trigger buffer, trigger, cancel while post-trigger buffer is filling");

            api.WaitForTransition("Waiting for pre-trigger buffer to fill", CAMERA_STATE.RUNNING, 10);
            api.ExpectState(CAMERA_STATE.RUNNING_PRETRIGGER_FULL);

            this.TriggerCamera(api, "Camera triggered, starting to fill post-trigger buffer");

            Console.WriteLine("    Canceling trigger while post-trigger buffer is filling");
            Console.WriteLine($"        {api.GetStatusString()}");

            api.Cancel();
            api.WaitForTransition("Waiting for cancel to be processed", CAMERA_STATE.TRIGGERED, 10);

            api.ExpectRunningState();
        }

        public void RunCaptureSaveStop(CamDictionary allowedSettings)
        {
            // Example showing partial pre-trigger buffer fill, trigger, save video, then truncate save leaving playable video file
            this.RunCamera(api, allowedSettings, "Partial pre-trigger buffer fill, trigger, start video save, then truncate save leaving playable video file");

            api.WaitForTransition("    Letting pre-trigger buffer partially fill", CAMERA_STATE.RUNNING, 10);
            Thread.Sleep(4000);
            Console.WriteLine($"        {api.GetStatusString()}");

            this.TriggerCamera(api, "Camera triggered, starting to fill post-trigger buffer");

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

        public void RunCaptureVideo(CamDictionary allowedSettings, string message, string baseFilename = null)
        {
            // Example showing full pre-trigger buffer fill, trigger, save video capture process
            this.RunCamera(api, allowedSettings, "Partial pre-trigger buffer fill, trigger, save video capture process");

            api.WaitForTransition("    Waiting for pre-trigger buffer to fill", CAMERA_STATE.RUNNING, 10);
            api.ExpectState(CAMERA_STATE.RUNNING_PRETRIGGER_FULL);

            this.TriggerCamera(api, "Camera triggered, filling post-trigger buffer", baseFilename);

            Console.WriteLine($"    Percentage pre-trigger buffer filled before trigger: {api.GetPretriggerFillLevel()}");

            api.WaitForTransition("Waiting for post-trigger buffer to fill", CAMERA_STATE.TRIGGERED, 10);
            api.ExpectState(CAMERA_STATE.SAVING);
            api.WaitForTransition("Waiting for save to complete", CAMERA_STATE.SAVING, 30);

            api.ExpectRunningState();
            Console.WriteLine(string.Format("    " + message, api.GetLastSavedFilename()));
        }
    }
}