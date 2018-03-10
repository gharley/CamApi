using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Newtonsoft.Json;

namespace CamApi
{
    public class CamApiLib
    {
        private static Dictionary<string, string> lookup = new Dictionary<string, string>(){
        {"Camera time:", "camera_time"},
        {"Model:", "model_string"},
        {"Sensitivity:", "iso"},
        {"Shutter:", "exposure"},
        {"Frame Rate:", "frame_rate"},
        {"Horizontal:", "horizontal"},
        {"Vertical:", "vertical"},
        {"Sub-sampling:", "subsample"},
        {"Duration:", "duration"},
        {"Pre-trigger:", "pretrigger"},
        {"Overclock:", "overclock"},
        {"Extended Dynamic Range:", "edr"},
        {"Genlock:", "genlock"},
        {"Force monochrome:", "force_monochrome"},
        {"Multishot Buffers:", "multishot_count"},
        {"External trigger debounce:", "trigger_debounce"},
        {"Gamma correction:", "gamma_correction"},
        {"Review before save:", "review"},
        {"Overlay notes:", "overlay_notes"},
        {"Overlay logo:", "overlay_logo"},
        {"Overlay settings:", "overlay_settings"},
        {"Overlay frame number:", "overlay_frame_number"},
        {"Active pipeline:", "pipeline"},
        {"Pipeline description:", "pipeline_description"},
        {"Multishot buffer:", "multishot_buffer"},
        {"Frame count:", "captured_frames"},
        {"Pre-trigger frames:", "pretrigger_frames"},
        {"Trigger time:", "trigger_time"},
        {"Trigger delay:", "trigger_to_exposure_delay"},
        {"First saved frame:", "first_saved_frame"},
        {"Last saved frame:", "last_saved_frame"},
        {"Genlock locked:", "genlock_locked"},
        {"DACVREFADC:", "dacvrefadc"},
        {"FPGA temp:", "fpga_temp"},
        {"FPGA temp at last calibration:", "calibration_fpga_temp"},
        {"Sensor temp:", "is_temp"},
        {"Sensor temp at last calibration:", "calibration_is_temp"},
        {"DDR3 temp:", "ddr3_temp"},
        {"DDR3 temp at last calibration:", "calibration_ddr3_temp"},
        {"Time since dark frame:", "time_since_dark_frame"},
        {"Time since power on:", "uptime"},
        {"Model number:", "model_number"},
        {"Serial number:", "serial_number"},
        {"Hardware revision:", "hardware_revision"},
        {"Hardware configuration:", "hardware_configuration"},
        {"Build date:", "build_date"},
        {"IR filter:", "ir_filter_installed"},
        {"Sensor type:", "sensor_type"},
        {"DDR3 memory size:", "memory_size"},
        {"Ethernet MAC address:", "ethernet_mac_address"},
        {"FPGA version:", "fpga_verson"},
        {"Software build date:", "software_build_date"},
        {"Software version:", "software_version"},
        };

        private string camAddr = null;

        public CamApiLib(string address)
        {
            camAddr = address;

            Console.WriteLine(string.Format("CAMAPI HTTP initialized.  Talking to camera: {0}", this.camAddr));
        }

        /**
          Internal low level and helper functions
        */

        private string AddFlagText(string msg, long flags, CAMAPI_FLAG flagBit, string text)
        {
            if ((flags & (long)flagBit) != 0)
            {
                if (string.IsNullOrEmpty(msg)) msg = text;
                else msg += " | " + text;
            }

            return msg;
        }

        // Returns data fetched from the target URL or None if HTTP returns an error trying to fetch the URL
        private string FetchTarget(string target)
        {
            string result = null;
            string url = "http://" + this.camAddr + target;

            try
            {
#if DEBUG
                Console.WriteLine($"    Fetching: {url}");
#endif

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

        // Accepts dictionary to be posted to uri.
        private string PostTarget(string target, CamDictionary data)
        {
            string result = null;
            string url = "http://" + this.camAddr + target;

            try
            {
#if DEBUG
                Console.WriteLine($"    Posting: {url}");
#endif

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

#if DEBUG                
                Console.WriteLine(string.Format("    Response: {0}", result));
#endif                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: unable to post: {url}");
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        private void PrintSettingLine(string label, object value)
        {
            Console.Write(label + ": ");
            if (value == null) Console.WriteLine("None");
            else Console.WriteLine($"{value:g6}");
        }

        private static string[] suffixes = { "bytes", "KB", "MB", "GB" };
        private string SizeofFmt(double num)
        {
            foreach (var suffix in suffixes)
            {
                if (num < 1024.0) return string.Format("{0:0.0} " + suffix, num);
                num /= 1024.0;
            }

            return string.Format("{0:0.0} TB", num);
        }

        public CAMAPI_STATUS Cancel()
        {
            // Cancel filling post-trigger buffer or save if either is occurring.
            // return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE
            string jdata = FetchTarget("/cancel");

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public bool CheckState(CAMERA_STATE desiredState)
        {
            // Returns True if camera is in desired_state.
            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), camStatus["state"]);

            return (state == desiredState);
        }

        public CamDictionary ConfigureCamera(CamDictionary settings)
        {
            // Configure camera using the requested_settings dictionary.
            // Calculates a camera configuration based on the requested values, camera limitations, and
            // a prioritized scheme to to eliminate inconsistencies.
            // return: dictionary of requested setting values along with the allowed values.
            string jdata = PostTarget("/configure_camera", settings);

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public void DeleteAllFavorites()
        {
            var ids = GetFavoriteIds();

            foreach (var id in ids)
            {
                var result = DeleteFavorite(id);

                Console.WriteLine($"result after delete: {result}");
            }
        }

        public CAMAPI_STATUS DeleteFavorite(string id)
        {
            // Deletes a previously saved favorite using id to identify which favorite to delete
            // returns: CAMAPI status
            string jdata = FetchTarget($"/delete_favorite?id={id}");

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
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
            var state = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), camStatus["state"]);

            if (state != CAMERA_STATE.RUNNING && state != CAMERA_STATE.RUNNING_PRETRIGGER_FULL)
            {
                Console.WriteLine($"Camera not in running state: {GetTextState(state)}");
                Environment.Exit(1);
            }
        }

        public void ExpectState(CAMERA_STATE anticipatedState)
        {
            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), camStatus["state"]);

            if (state != anticipatedState)
            {
                Console.WriteLine($"Camera not in expected state: anticipated {GetTextState(anticipatedState)} != {GetTextState(state)}");
                Environment.Exit(1);
            }
        }

        public CAMAPI_STATUS EraseAllFiles(string device = null)
        {
            // Erases all the files in the DCIM directory on the active storage device if dev=None.
            // You can use dev="USB" or dev="SD" to specify device to have files erased.
            // return: outcome, either CAMAPI.STATUS_OKAY or CAMAPI_STATUS.STORAGE_ERROR
            string url = "/erase_all_files";

            if (device != null) url += $"?device={device}";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS FormatStorage(string device = null)
        {
            // Formats active storage device is dev is not specified.  Use dev="USB" or dev="SD" to specify device to be formatted.
            // return: outcome, either CAMAPI.STATUS_OKAY or CAMAPI_STATUS.STORAGE_ERROR
            string url = "/format";

            if (device != null) url += $"?device={device}";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CamDictionary GetCamInfo()
        {
            // Returns a dictionary of all the unchanging camera information.
            string jdata = FetchTarget("/get_caminfo");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public CamDictionary GetCamStatus()
        {
            // Returns camera status dictionary.
            string jdata = FetchTarget("/get_camstatus");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public CamDictionary GetCurrentSettings()
        {
            // Returns dictionary containing the current requested camera settings that are being used if camera is active,
            // otherwise None. Dictionary may contain other values - do not count on them being present in future versions of CAMAPI.
            // Defined camera settings documented at http://wiki.edgertronic.com/index.php/Software_developers_kit
            string jdata = FetchTarget("/get_current_settings");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public CamDictionary GetFavorite(string id)
        {
            // returns: dictionary containing previously saved favorite settings with identifier id.
            string jdata = FetchTarget($"/get_favorite?id={id}");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public List<string> GetFavoriteIds()
        {
            // returns: list of saved favorite setting identifiers, will be an empty list if no settings have been saved.
            var jdata = FetchTarget("/get_favorite_ids");

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

        public string GetLastSavedFilename(){
            // Returns filename used by the last successful video capture.
            // Note: API is deprecated, better to query file system directly.
            return FetchTarget("/get_last_saved_filename");
        }

        public long GetPretriggerFillLevel()
        {
            // Returns accurate value of the actual pretrigger buffer fill level.
            string jdata = FetchTarget("/pretrigger_buffer_fill_level");

            return (long)JsonConvert.DeserializeObject(jdata);
        }

        public CamDictionary GetSavedSettins(string id = null)
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

        public string GetStatusString()
        {
            // Returns human readable string of the current device status.  Text format will change in the future.
            string result;
            var camStatus = GetCamStatus();
            var state = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), camStatus["state"]);

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

        public string GetStorageDir()
        {
            // Returns path to mount point of the active storage device or None if there is no storage device available.
            string jdata = FetchTarget("/get_storage_dir");

            return (string)JsonConvert.DeserializeObject(jdata);
        }

        public CamDictionary GetStorageInfo(string device = null)
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

        public CAMAPI_STATUS Mount(string device = null)
        {
            // Attempts to mount specified storage device. Use /mount?device=USB,
            // /mount?device=SD, or /mount?device=NET to specify the device.
            // Returns: CAMAPI_STATUS.OKAY or CAMAPI_STATUS.STORAGE_ERROR, CAMAPI_STATUS.INVALID_PARAMETER
            string url = "/mount";

            if (device != null) url += $"?device={device}";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
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

        public CAMAPI_STATUS Run(CamDictionary settings)
        {
            // Reconfigures the camara to use the best match values based on the requested values,
            // calibrates the camera using those values, and starts capturing the pre-trigger video frames.
            // The best match values are the same balues as those returned by configure_camera().
            // return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE

            string jdata = PostTarget("/run", settings);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS Save()
        {
            // Saves videos when multishot capture is enabled and one or more multishot buffers contain unsaved videos.
            // :return: outcome, either CAMAPI_STATUS.OKAY or CAMAPI_STATUS.INVALID_STATE
            string url = "/save";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS SaveFavorite(CamDictionary settings)
        {
            // Save a set of camera settings.  The supplied settings must have an id key set to a valid value.
            // returns: CAMAPI_STATUS.OKAY, CAMAPI_STATUS.INVALID_PARAMETER, or CAMAPI_STATUS.STORAGE_ERROR
            string jdata = PostTarget("/save_favorite", settings);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS SaveStop(bool discardUnsaved = false)
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

        public CAMAPI_STATUS SelectiveSave(CamDictionary parameters)
        {
            // Save portion of video previously stored in DDR3 memory.  Captured videos in DDR3 are not modified.
            // :param parameters['buffer_index']: which multishot buffer to save.
            // :param parameters['start_frame']: Starting frame to save.
            // :param parameters['end_frame']: Ending frame to save.
            // :param parameters['filename']: Optional, specify base filename (no path, no suffix) used for saving video and metadata
            // :return: outcome, CAMAPI_STATUS.OKAY, CAMAPI_STATUS.INVALID_STATE, CAMAPI_STATUS.INVALID_PARAMETER
            string jdata = PostTarget("/selective_save", parameters);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CAMAPI_STATUS Trigger(string baseFilename = null)
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

        public CAMAPI_STATUS Unmount(string device = null)
        {
            // Attempts to unmount mnt_pt or selected storage device if dev is None.
            // Use dev="USB" or dev="SD" to specify device to be unmounted. Unmounting
            // the selected storage device causes the camera to select USB
            // storage if possible or SD storage if USB storage is unavailable and
            // SD storage is usable.
            // Returns: CAMAPI_STATUS.OKAY or CAMAPI_STATUS.STORAGE_ERROR
            string url = "/unmount";

            if (device != null) url += $"?device={device}";

            string jdata = FetchTarget(url);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
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
            var state = (CAMERA_STATE)Enum.ToObject(typeof(CAMERA_STATE), camStatus["state"]);

            Console.WriteLine($"        Transition complete - new state: {GetTextState(state)}");
        }
    }
}