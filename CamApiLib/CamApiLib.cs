using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

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

        private string GetTextState(CAMERA_STATE state)
        {
            string result;

            switch (state)
            {
                case CAMERA_STATE.UNCONFIGURED:
                    result = "Unconfigured";
                    break;
                case CAMERA_STATE.CALIBRATING:
                    result = "Calibrating";
                    break;
                case CAMERA_STATE.RUNNING:
                    result = "Running";
                    break;
                case CAMERA_STATE.RUNNING_PRETRIGGER_FULL:
                    result = "Running pretrigger buffer full";
                    break;
                case CAMERA_STATE.TRIGGERED:
                    result = "Triggered";
                    break;
                case CAMERA_STATE.SAVING:
                    result = "Saving";
                    break;
                case CAMERA_STATE.TRIGGER_CANCELED:
                    result = "Trigger canceled";
                    break;
                case CAMERA_STATE.SAVE_CANCELED:
                    result = "Save canceled";
                    break;
                case CAMERA_STATE.SAVE_INTERRUPTED:
                    result = "Save interrupted";
                    break;
                case CAMERA_STATE.SAVE_TRUNCATING:
                    result = "Save truncating";
                    break;
                case CAMERA_STATE.REVIEWING:
                    result = "Reviewing";
                    break;
                case CAMERA_STATE.SELECTIVE_SAVING:
                    result = "Selective saving";
                    break;
                default:
                    result = "Logic error - unknown state";
                    break;
            }
            return result;
        }

        // Accepts dictionary to be posted to uri.
        private string PostTarget(string target, Dictionary<string, object> data)
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
            else Console.WriteLine($"{value}");
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

        public Dictionary<string, object> ConfigureCamera(Dictionary<string, Object> settings)
        {
            // Configure camera using the requested_settings dictionary.
            // Calculates a camera configuration based on the requested values, camera limitations, and
            // a prioritized scheme to to eliminate inconsistencies.
            // return: dictionary of requested setting values along with the allowed values.
            string jdata = PostTarget("/configure_camera", settings);

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
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

        public int DeleteFavorite(string id)
        {
            // Deletes a previously saved favorite using id to identify which favorite to delete
            // returns: CAMAPI status
            return int.Parse(FetchTarget($"/delete_favorite?id={id}"));
        }

        public Dictionary<string, object> GetCamInfo()
        {
            // Returns a dictionary of all the unchanging camera information.
            string jdata = FetchTarget("/get_caminfo");

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
        }

        public Dictionary<string, object> GetCamStatus()
        {
            // Returns camera status dictionary.
            string jdata = FetchTarget("/get_camstatus");

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
        }

        public Dictionary<string, object> GetCurrentSettings()
        {
            // Returns dictionary containing the current requested camera settings that are being used if camera is active,
            // otherwise None. Dictionary may contain other values - do not count on them being present in future versions of CAMAPI.
            // Defined camera settings documented at http://wiki.edgertronic.com/index.php/Software_developers_kit
            string jdata = FetchTarget("/get_current_settings");

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
        }

        public Dictionary<string, object> GetFavorite(string id)
        {
            // returns: dictionary containing previously saved favorite settings with identifier id.
            string jdata = FetchTarget($"/get_favorite?id={id}");

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
        }

        public List<string> GetFavoriteIds()
        {
            // returns: list of saved favorite setting identifiers, will be an empty list if no settings have been saved.
            var jdata = FetchTarget("/get_favorite_ids");

            return (List<string>)JsonConvert.DeserializeObject(jdata, typeof(List<string>));
        }

        public int GetPretriggerFillLevel()
        {
            // Returns accurate value of the actual pretrigger buffer fill level.
            string jdata = FetchTarget("/pretrigger_buffer_fill_level");

            return (int)JsonConvert.DeserializeObject(jdata);
        }

        public Dictionary<string, object> GetSavedSettins(string id = null)
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

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
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

        public Dictionary<string, object> GetStorageInfo(string device = null)
        {
            // Returns a dictonary containing information about the storage device , or about active storage device if device is not set
            string url = "/get_storage_info";

            if (!string.IsNullOrEmpty(device))
            {
                url += "?device=" + device;
            }

            string jdata = FetchTarget(url);

            return (Dictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(Dictionary<string, object>));
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

        public void PrintSettings(Dictionary<string, object> settings, string keyPrefix, string prefix)
        {
            PrintSettingLine(prefix + "Sensitivity", settings[keyPrefix + "iso"]);

            var exposure = (double)settings[keyPrefix + "exposure"];
            if (exposure == 0) Console.WriteLine($"{prefix}Shutter: None");
            else Console.WriteLine($"{prefix}Shutter: {1 / exposure:0.00}");

            PrintSettingLine(prefix + "Frame Rate", settings[keyPrefix + "frame_rate"]);
            PrintSettingLine(prefix + "Horizontal", settings[keyPrefix + "horizontal"]);
            PrintSettingLine(prefix + "Vertical", settings[keyPrefix + "vertical"]);

            Console.Write($"{prefix}Sub-sampling: ");
            Console.WriteLine((!settings.ContainsKey(keyPrefix + "subsample") || (long)settings[keyPrefix + "subsample"] == 0) ? "Off" : "On");

            PrintSettingLine(prefix + "Duration", settings[keyPrefix + "duration"]);
            PrintSettingLine(prefix + "Pre-trigger", settings[keyPrefix + "pretrigger"]);
        }

        public int Run(Dictionary<string, object> settings)
        {
            // Reconfigures the camara to use the best match values based on the requested values,
            // calibrates the camera using those values, and starts capturing the pre-trigger video frames.
            // The best match values are the same balues as those returned by configure_camera().
            // return: outcome, either CAMAPI_STATUS_OKAY or CAMAPI_STATUS_INVALID_STATE

            return int.Parse(PostTarget("/run", settings));
        }

        public int SaveFavorite(Dictionary<string, object> settings)
        {
            // Save a set of camera settings.  The supplied settings must have an id key set to a valid value.
            // returns: CAMAPI status okay, illegal parameter, or storage error
            return int.Parse(PostTarget("/save_favorite", settings));
        }
    }
}