using System.Collections.Generic;

namespace CamApi
{
  public enum CAMERA_STATE
  {
    UNCONFIGURED = 1, CALIBRATING, RUNNING, TRIGGERED, SAVING, RUNNING_PRETRIGGER_FULL, TRIGGER_CANCELED,
    SAVE_CANCELED, SAVE_INTERRUPTED, SAVE_TRUNCATING, REVIEWING, SELECTIVE_SAVING
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
    SD_CARD_STORAGE_UNMOUNTED = 0x000080,
    USB_STORAGE_UNMOUNTED = 0x000100,
    NET_CONFIGURED = 0x000200,
    NET_NOT_MOUNTABLE = 0x000400,
    NET_FULL = 0x000800,
    GENLOCK_NO_SIGNAL = 0x400000,
    GENLOCK_CONFIG_ERROR = 0x800000
  }

  public enum CAMAPI_STATUS
  {
    OKAY = 1, INVALID_STATE, STORAGE_ERROR, CODE_OUT_OF_DATE, INVALID_PARAMETER
  };

  public class CamDictionary : Dictionary<string, object> { }

  internal static class CamConstants
  {
    public static Dictionary<string, string> METADATA_FILE_LOOKUP_TABLE = new Dictionary<string, string>(){
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

    public static List<string> METADATA_STRING_KEYS = new List<string>(){
        "camera_time",
        "model_string",
        "pipeline_description",
        "time_since_dark_frame",
        "uptime",
        "software_version",
        "ethernet_mac_address"
    };

    public static List<string> METADATA_ON_OFF_KEYS = new List<string>(){
        "subsample", 
        "force_monochrome", 
        "overlay_notes", 
        "overlay_logo",
        "overlay_settings", 
        "overlay_frame_number", 
        "trigger_debounce",
        "gamma_correction", 
        "review"
    };
    
  }
}