using System;
using System.Threading;

using CamApi;

namespace CamApiCli
{
    public static class Extensions
    {
        private static void DoRunCamera(CamApiLib api, CamDictionary allowedSettings, string message)
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

        private static void DoTriggerCamera(CamApiLib api, string message, string baseFilename = null)
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

        public static void RunCamera(this CaptureTests _this, CamApiLib api, CamDictionary allowedSettings, string message)
        {
            DoRunCamera(api, allowedSettings, message);
        }

        public static void RunCamera(this MultiCaptureTests _this, CamApiLib api, CamDictionary allowedSettings, string message)
        {
            DoRunCamera(api, allowedSettings, message);
        }

        public static void TriggerCamera(this CaptureTests _this, CamApiLib api, string message, string baseFilename = null)
        {
            DoTriggerCamera(api, message, baseFilename);
        }

        public static void TriggerCamera(this MultiCaptureTests _this, CamApiLib api, string message, string baseFilename = null)
        {
            DoTriggerCamera(api, message, baseFilename);
        }
    }
}