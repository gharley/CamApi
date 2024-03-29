using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Newtonsoft.Json;
using CamApiExtensions;

namespace CamApi
{
    public class CamApiLib
    {
        private string camAddr = null;
        private bool debug;

        public CamApiLib(string address, bool debug = false)
        {
            camAddr = address;
            this.debug = debug;
            if (debug)
                Console.WriteLine($"CAMAPI HTTP initialized.  Talking to camera: {camAddr}");
        }

        /******************************************************************************
          Helper methods
        ******************************************************************************/

        // Returns data fetched from the target URL or None if HTTP returns an error trying to fetch the URL
        public string FetchTarget(string target)
        {
            string result = null;
            string url = "http://" + this.camAddr + target;

            try
            {
                if (debug)
                    Console.WriteLine($"    Fetching: {url}");

                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());

                result = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: unable to fetch: {url}");
                Console.WriteLine(ex.Message);
            }

            if (debug)
                Console.WriteLine($"        Response: {result}");

            return result;
        }

        // Accepts dictionary to be posted to uri.
        public string PostTarget(string target, CamDictionary data)
        {
            string result = null;
            string url = "http://" + this.camAddr + target;

            try
            {
                if (debug)
                    Console.WriteLine($"    Posting: {url}");

                WebRequest request = WebRequest.Create(url);
                string jsonData = JsonConvert.SerializeObject(data);

                request.Method = "POST";
                request.ContentType = "application/json";

                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(jsonData);
                    writer.Flush();
                    writer.Close();
                }

                var response = request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());

                result = reader.ReadToEnd();

                if (debug)
                    Console.WriteLine($"    Response: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: unable to post: {url}");
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        /******************************************************************************
                The following utility methods are not strictly part of CAMAPI
        ******************************************************************************/

        private string AddFlagText(string msg, long flags, CAMAPI_FLAG flagBit, string text)
        {
            if ((flags & (long)flagBit) != 0)
            {
                if (string.IsNullOrEmpty(msg)) msg = text;
                else msg += " | " + text;
            }

            return msg;
        }

        public bool CheckState(CAMERA_STATE desiredState)
        {
            // Returns True if camera is in desired_state.
            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)camStatus["state"];

            return (state == desiredState);
        }

        public void DisplayRemoteFile(string remoteFQN)
        {
            // Downloads file and prints the content on stdout.  Downloads using the remote
            // fully qualified name.
            string data = FetchTarget($"/static{remoteFQN}");

            Console.WriteLine(data);
        }

        public void ExpectRunningState()
        {
            // With smart calibrate, the camera may or may not be in the calibrating state.
            // If calibrating, wait for caibration to complete before returning.
            int timeout = 4;

            while (CheckState(CAMERA_STATE.CALIBRATING) && timeout > 0)
            {
                timeout--;
                Console.WriteLine($"    {GetStatusString()}");
                Thread.Sleep(1000);
            }

            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)camStatus["state"];

            if (state != CAMERA_STATE.RUNNING && state != CAMERA_STATE.RUNNING_PRETRIGGER_FULL)
            {
                Console.WriteLine($"Camera not in running state: {GetTextState(state)}");
                Environment.Exit(1);
            }
        }

        public void ExpectState(CAMERA_STATE anticipatedState)
        {
            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)camStatus["state"];

            if (state != anticipatedState)
            {
                Console.WriteLine($"Camera not in expected state: anticipated {GetTextState(anticipatedState)} != {GetTextState(state)}");
                Environment.Exit(1);
            }
        }

        public List<string> FetchRemoteDirectoryListing(string path = null)
        {
            // Returns a list of filenames.  If path is None, then the list is the files in the video directories
            // stored on the active storage device otherwise returns list of files in the named path.
            string url = "/dir_listing";

            if (!string.IsNullOrEmpty(path)) url += $"?{path}";

            string jdata = FetchTarget(url);

            return (List<string>)JsonConvert.DeserializeObject(jdata, typeof(List<string>));
        }

        private static Dictionary<string, string> infoLookup = new Dictionary<string, string>(){
          {"sw_build_date", "Software build date"},
          {"build_date", "Hardware build date"},
          {"fpga_version", "FPGA version"},
          {"model_number", "Model Number"},
          {"serial_number", "Serial Number"},
          {"hardware_revision", "Hardware Revision"},
          {"hardware_configuration", "Hardware Configuration"}
        };

        public string GetInfoString(string prefix)
        {
            // Returns human readable text describing the camera.  Text format will change in the future.
            var info = GetCamInfo();
            string result = string.Join(prefix, from kv in infoLookup
                                                where info.ContainsKey(kv.Key)
                                                select $"{kv.Value}: {info[kv.Key]}\n");

            if (info.ContainsKey("ir_filter"))
                result += prefix + $"IR Filter: {((long)info["ir_filter"] != 0 ? "" : "not ")} installed\n";

            result += $"{prefix}Ethernet MAC Address: {info["mac_addr"]}\n";

            return result;
        }

        private string GetTextFlags(long flags)
        {
            // Helper routine to present flags as human readable text.
            string result = null;

            result = AddFlagText(result, flags, CAMAPI_FLAG.STORAGE_FULL, "storage full");
            result = AddFlagText(result, flags, CAMAPI_FLAG.STORAGE_MISSING_OR_UNMOUNTED, "storage missing or unmounted");
            result = AddFlagText(result, flags, CAMAPI_FLAG.USB_STORAGE_INSTALLED, "USB storage installed");
            result = AddFlagText(result, flags, CAMAPI_FLAG.SD_CARD_STORAGE_INSTALLED, "SD card installed");
            result = AddFlagText(result, flags, CAMAPI_FLAG.USB_STORAGE_FULL, "USB storage full");
            result = AddFlagText(result, flags, CAMAPI_FLAG.SD_CARD_STORAGE_FULL, "SD card full");
            result = AddFlagText(result, flags, CAMAPI_FLAG.STORAGE_BAD, "Storage unusable");
            result = AddFlagText(result, flags, CAMAPI_FLAG.USB_STORAGE_UNMOUNTED, "USB storage unmounted");
            result = AddFlagText(result, flags, CAMAPI_FLAG.SD_CARD_STORAGE_UNMOUNTED, "SD card unmounted");
            result = AddFlagText(result, flags, CAMAPI_FLAG.NET_CONFIGURED, "Network storage configured");
            result = AddFlagText(result, flags, CAMAPI_FLAG.NET_NOT_MOUNTABLE, "Network storage unmountable");
            result = AddFlagText(result, flags, CAMAPI_FLAG.NET_FULL, "Network storage full");
            result = AddFlagText(result, flags, CAMAPI_FLAG.GENLOCK_NO_SIGNAL, "genlock signal not detected");
            result = AddFlagText(result, flags, CAMAPI_FLAG.GENLOCK_CONFIG_ERROR, "genlock config error");

            return result;
        }

        private void PrintSettingLine(string label, object value)
        {
            Console.Write(label + ": ");
            if (value == null) Console.WriteLine("None");
            else Console.WriteLine($"{value:g6}");
        }

        public void PrintSettings(CamDictionary settings, string keyPrefix, string prefix)
        {
            PrintSettingLine(prefix + "Sensitivity", settings[keyPrefix + "iso"]);

            var exposure = (double)settings[keyPrefix + "exposure"];
            if (exposure == 0) Console.WriteLine($"{prefix}Shutter: None");
            else Console.WriteLine($"{prefix}Shutter: {1 / exposure:g6}");

            PrintSettingLine(prefix + "Frame Rate", settings[keyPrefix + "frame_rate"]);
            PrintSettingLine(prefix + "Horizontal", settings[keyPrefix + "horizontal"]);
            PrintSettingLine(prefix + "Vertical", settings[keyPrefix + "vertical"]);

            Console.Write($"{prefix}Sub-sampling: ");
            Console.WriteLine((!settings.ContainsKey(keyPrefix + "subsample") || (long)settings[keyPrefix + "subsample"] == 0) ? "Off" : "On");

            PrintSettingLine(prefix + "Duration", settings[keyPrefix + "duration"]);
            PrintSettingLine(prefix + "Pre-trigger", settings[keyPrefix + "pretrigger"]);
        }

        private static Dictionary<CAMERA_STATE, string> TextStateLookup = new Dictionary<CAMERA_STATE, string>(){
          {CAMERA_STATE.CALIBRATING, "Calibrating"},
          {CAMERA_STATE.REVIEWING, "Reviewing"},
          {CAMERA_STATE.RUNNING, "Running"},
          {CAMERA_STATE.RUNNING_PRETRIGGER_FULL, "Running pretrigger buffer full"},
          {CAMERA_STATE.SELECTIVE_SAVING, "Selective saving"},
          {CAMERA_STATE.SAVING, "Saving"},
          {CAMERA_STATE.SAVE_CANCELED, "Save canceled"},
          {CAMERA_STATE.SAVE_INTERRUPTED, "Save interrupted"},
          {CAMERA_STATE.SAVE_TRUNCATING, "Save truncating"},
          {CAMERA_STATE.TRIGGERED, "Triggered"},
          {CAMERA_STATE.TRIGGER_CANCELED, "Trigger canceled"},
          {CAMERA_STATE.UNCONFIGURED, "Unconfigured"},
        };

        public string GetTextState(CAMERA_STATE state)
        {
            return TextStateLookup.ContainsKey(state) ? TextStateLookup[state] : "Logic error - unknown state";
        }

        public string GetStatusString()
        {
            // Returns human readable string of the current device status.  Text format will change in the future.
            string result;
            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)camStatus["state"];

            result = $"State: {GetTextState(state)}; Level: {camStatus["level"]}; " +
              $"Flags: {GetTextFlags((long)camStatus["flags"])}; Empty: {SizeofFmt((double)((long)camStatus["available_space"]))}";

            if (camStatus.ContainsKey("active_buffer"))
            {
                var activeBuffer = (long)camStatus["active_buffer"];

                if (camStatus.ContainsKey("captured_buffers"))
                {
                    var capturedBuffers = (long)camStatus["captured_buffers"];

                    switch (state)
                    {
                        case CAMERA_STATE.SAVING:
                            result += $"; Saving multishot: {activeBuffer}/{capturedBuffers}";
                            break;
                        case CAMERA_STATE.SELECTIVE_SAVING:
                            result += $"; Selective saving multishot: {activeBuffer}/{capturedBuffers}";
                            break;
                        case CAMERA_STATE.REVIEWING:
                            result += $"; Reviewing multishot: {activeBuffer}";
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var activeSettings = GetCurrentSettings();

                    if (state == CAMERA_STATE.TRIGGERED)
                    {
                        result += $"; Capturing multishot: {activeBuffer}/{activeSettings["multishot_count"]}";
                    }
                    else
                    {
                        result += $"; Pre-filling multishot: {activeBuffer}/{activeSettings["multishot_count"]}";
                    }
                }
            }

            return result;
        }

        private static string[] suffixes = { "bytes", "KB", "MB", "GB" };

        private string SizeofFmt(double num)
        {
            foreach (var suffix in suffixes)
            {
                if (num < 1024.0) return $"{num:0.0} {suffix}";
                num /= 1024.0;
            }

            return $"{num:0.0} TB";
        }

        public long SyncTime(DateTime? setTime = null)
        {
            // Returns the camera's current time in seconds from Jan 1, 1970 (called the Unix epoch).
            // If set_time is specified (as C# DateTime), then the camera's hardware real time clock is first set.
            string url = "/sync_time";

            if (setTime != null) url += $"?{setTime.ToUnixTime()}";

            string jdata = FetchTarget(url);

            return long.Parse(jdata);
        }

        public void WaitForTransition(string label, CAMERA_STATE currentState, int timeout)
        {
            Console.WriteLine($"    {label}");

            while (CheckState(currentState) && timeout > 0)
            {
                timeout--;
                Console.WriteLine($"        {GetStatusString()}");
                Thread.Sleep(1000);
            }

            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)camStatus["state"];

            Console.WriteLine($"        Transition complete - new state: {GetTextState(state)}");
        }

        /******************************************************************************
            API methods
        ******************************************************************************/
        public CAMAPI_STATUS Cancel() // API
        {
            // Cancel filling post-trigger buffer or save if either is occurring.
            // return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE
            string jdata = FetchTarget("/cancel");

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CamDictionary ConfigureCamera(CamDictionary settings) // API
        {
            // Configure camera using the requested_settings dictionary.
            // Calculates a camera configuration based on the requested values, camera limitations, and
            // a prioritized scheme to to eliminate inconsistencies.
            // return: dictionary of requested setting values along with the allowed values.
            string jdata = PostTarget("/configure_camera", settings);

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public CAMAPI_STATUS EraseAllFiles(string device = null) // API
        {
            // Erases all the files in the DCIM directory on the active storage device if dev=None.
            // You can use dev="USB" or dev="SD" to specify device to have files erased.
            // return: outcome, either CAMAPI.STATUS_OKAY or CAMAPI_STATUS.STORAGE_ERROR
            string url = "/erase_all_files";

            if (device != null) url += $"?device={device}";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CamDictionary GetCamInfo() // API
        {
            // Returns a dictionary of all the unchanging camera information.
            string jdata = FetchTarget("/get_caminfo");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public CamDictionary GetCamStatus() // API
        {
            // Returns camera status dictionary.
            string jdata = FetchTarget("/get_camstatus");
            CamDictionary result = (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));

            result["state"] = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), result["state"]);

            return result;
        }

        public CamDictionary GetCurrentSettings() // API
        {
            // Returns dictionary containing the current requested camera settings that are being used if camera is active,
            // otherwise None. Dictionary may contain other values - do not count on them being present in future versions of CAMAPI.
            // Defined camera settings documented at http://wiki.edgertronic.com/index.php/Software_developers_kit
            string jdata = FetchTarget("/get_current_settings");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public string GetLastSavedFilename() // API
        {
            // Returns filename used by the last successful video capture.
            // Note: API is deprecated, better to query file system directly.
            return FetchTarget("/get_last_saved_filename");
        }

        public long GetPretriggerFillLevel() // API
        {
            // Returns accurate value of the actual pretrigger buffer fill level.
            string jdata = FetchTarget("/pretrigger_buffer_fill_level");

            return (long)JsonConvert.DeserializeObject(jdata);
        }

        public CamDictionary GetSavedSettings(string id = null) // API
        {
            // Returns dictionary containing last successfully saved settings or
            // default camera settings otherwise.  If id is specified, returns the
            // camera settings for that identifier, or an empty dictionary if the
            // settings for the specified identifier do not exist.
            string url = "/get_saved_settings";

            if (!string.IsNullOrEmpty(id))
            {
                url += "?id=" + id;
            }

            string jdata = FetchTarget(url);

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public string GetStorageDir() // API
        {
            // Returns path to mount point of the active storage device or None if there is no storage device available.
            string jdata = FetchTarget("/get_storage_dir");

            return (string)JsonConvert.DeserializeObject(jdata);
        }

        public CamDictionary GetStorageInfo(string device = null) // API
        {
            // Returns a dictonary containing information about the storage device , or about active storage device if device is not set
            string url = "/get_storage_info";

            if (!string.IsNullOrEmpty(device))
            {
                url += "?device=" + device;
            }

            string jdata = FetchTarget(url);

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public CAMAPI_STATUS Run(CamDictionary settings) // API
        {
            // Reconfigures the camara to use the best match values based on the requested values,
            // calibrates the camera using those values, and starts capturing the pre-trigger video frames.
            // The best match values are the same balues as those returned by configure_camera().
            // return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE

            string jdata = PostTarget("/run", settings);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS SaveStop(bool discardUnsaved = false) // API
        {
            // Stop save that is in process, truncating video to the portion saved so far.  Rest of captured video data is discarded.
            // If there are more unsaved multishot videos and discard_unsaved is set, the rest of the unsaved videos are
            // discarded as well.
            // :return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE
            string url = "/save_stop";

            if (discardUnsaved) url += $"?discard_unsaved=1";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS Trigger(string baseFilename = null) // API
        {
            // Stops filling the pre-trigger portion buffer and starts filling the post-trigger portion of the buffer.
            // The base filename will have a ".mov"
            // extension appended for the movie file and a ".txt" extension appended for the file containing
            // the video capture metadata.  The file will be saved it the active storage device unless the
            // baseFilename starts with a slash ('/'), in which case the baseFilename is used without
            // prepending the path to the active storage device.

            // :param baseFilename: Name of file to use to hold the captured video.  baseFilename should not
            //                     have an extension.  If the baseFilename begins with '/', then the path
            //                     to the active storage device is not prepended.
            // :return: outcome, either CAMAPI.STATUS_OKAY or CAMAPI_STATUS.INVALID_STATE
            string url = "/trigger";

            if (baseFilename != null) url += $"?base_filename={baseFilename}";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }
    }
}