using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

namespace CamApi
{
    public class CamApiLib

    {

        public enum CAMERA_STATE
        {
            CAMAPI_STATE_UNCONFIGURED = 1, CAMAPI_STATE_CALIBRATING, CAMAPI_STATE_RUNNING, CAMAPI_STATE_TRIGGERED, CAMAPI_STATE_SAVING,
            CAMAPI_STATE_RUNNING_PRETRIGGER_FULL, CAMAPI_STATE_TRIGGER_CANCELED, CAMAPI_STATE_SAVE_CANCELED, CAMAPI_STATE_SAVE_INTERRUPTED,
            CAMAPI_STATE_SAVE_TRUNCATING, CAMAPI_STATE_REVIEWING, CAMAPI_STATE_SELECTIVE_SAVING
        };

        public enum CAMAPI_FLAG
        {
            STORAGE_FULL = 0x000001,
            STORAGE_MISSING_OR_UNMOUNTED = 0x000002,
            USB_STORAGE_INSTALLED = 0x000004,
            SD_CARD_STORAGE_INSTALLED = 0x000008,
            USB_STORAGE_FULL = 0x000010,
            SD_CARD_STORAGE_FULL = 0x000020,
            STORAGE_BAD = 0x000040,
            NET_CONFIGURED = 0x000080,
            NET_NOT_MOUNTABLE = 0x000100,
            NET_FULL = 0x000200,
            USB_STORAGE_UNMOUNTED = 0x000400,
            SD_CARD_STORAGE_UNMOUNTED = 0x008000,
            GENLOCK_NO_SIGNAL = 0x400000,
            GENLOCK_CONFIG_ERROR = 0x800000
        }

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

        // Accepts dictionary to be posted to uri.
        private string PostTarget(string target, IDictionary<string, object> data)
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
                Console.WriteLine(string.Format("    Response: {0}", result));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: unable to post: {url}");
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        private string AddFlagText(string msg, long flags, CAMAPI_FLAG flagBit, string text)
        {
            if ((flags & (long)flagBit) != 0)
            {
                if (string.IsNullOrEmpty(msg)) msg = text;
                else msg += " | " + text;
            }

            return msg;
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
                case CAMERA_STATE.CAMAPI_STATE_UNCONFIGURED:
                    result = "Unconfigured";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_CALIBRATING:
                    result = "Calibrating";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_RUNNING:
                    result = "Running";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_RUNNING_PRETRIGGER_FULL:
                    result = "Running pretrigger buffer full";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_TRIGGERED:
                    result = "Triggered";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_SAVING:
                    result = "Saving";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_TRIGGER_CANCELED:
                    result = "Trigger canceled";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_SAVE_CANCELED:
                    result = "Save canceled";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_SAVE_INTERRUPTED:
                    result = "Save interrupted";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_SAVE_TRUNCATING:
                    result = "Save truncating";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_REVIEWING:
                    result = "Reviewing";
                    break;
                case CAMERA_STATE.CAMAPI_STATE_SELECTIVE_SAVING:
                    result = "Selective saving";
                    break;
                default:
                    result = "Logic error - unknown state";
                    break;
            }
            return result;
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

        public IDictionary<string, object> GetCamStatus()
        {
            // Returns camera status dictionary.
            string jdata = FetchTarget("/get_camstatus");

            return (IDictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(IDictionary<string, object>));
        }

        public IDictionary<string, object> GetCurrentSettings()
        {
            // Returns dictionary containing the current requested camera settings that are being used if camera is active,
            // otherwise None. Dictionary may contain other values - do not count on them being present in future versions of CAMAPI.
            // Defined camera settings documented at http://wiki.edgertronic.com/index.php/Software_developers_kit
            string jdata = FetchTarget("/get_current_settings");

            return (IDictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(IDictionary<string, object>));
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
                        case CAMERA_STATE.CAMAPI_STATE_SAVING:
                            result += $"; Saving multishot: {activeBuffer}/{capturedBuffers}";
                            break;
                        case CAMERA_STATE.CAMAPI_STATE_SELECTIVE_SAVING:
                            result += $"; Selective saving multishot: {activeBuffer}/{capturedBuffers}";
                            break;
                        case CAMERA_STATE.CAMAPI_STATE_REVIEWING:
                            result += $"; Reviewing multishot: {activeBuffer}";
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var activeSettings = GetCurrentSettings();

                    if (state == CAMERA_STATE.CAMAPI_STATE_TRIGGERED)
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

        /*
            def get_storage_dir(self):
                """
                Returns path to mount point of the active storage device or None if there is no storage device available.
                """
                jdata = self._fetch_target('/get_storage_dir')
                return yaml.load(jdata)
         */
        public int GetPretriggerFillLevel()
        {
            // Returns accurate value of the actual pretrigger buffer fill level.
            string jdata = FetchTarget("/pretrigger_buffer_fill_level");

            return (int)JsonConvert.DeserializeObject(jdata);
        }

        public string GetStorageDir()
        {
            // Returns path to mount point of the active storage device or None if there is no storage device available.
            string jdata = FetchTarget("/get_storage_dir");

            return (string)JsonConvert.DeserializeObject(jdata);
        }

        public IDictionary<string, object> GetStorageInfo(string device = null)
        {
            // Returns a dictonary containing information about the storage device , or about active storage device if device is not set
            string url = "/get_storage_info";

            if(!string.IsNullOrEmpty(device)){
              url += "?device=" + device;
            }

            string jdata = FetchTarget(url);

            return (IDictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(IDictionary<string, object>));
        }
    }
}