using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

namespace CamAPILib
{
  public class CamAPI

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

    public CamAPI(string address)
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
        Console.WriteLine(string.Format("    Fetching: {0}", url));

        WebRequest request = WebRequest.Create(url);
        WebResponse response = request.GetResponse();
        var reader = new StreamReader(response.GetResponseStream());

        result = reader.ReadToEnd();
      }
      catch (Exception ex)
      {
        Console.WriteLine("ERROR: unable to fetch: " + url);
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
        Console.WriteLine(string.Format("    Posting: {0}", url));

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
        Console.WriteLine("ERROR: unable to post: " + url);
        Console.WriteLine(ex.Message);
      }

      return result;
    }
    public IDictionary<string, object> GetCamStatus()
    {
      // Returns camera status dictionary.
      string jdata = FetchTarget("/get_camstatus");

      return (IDictionary<string, object>)JsonConvert.DeserializeObject(jdata, typeof(IDictionary<string, object>));
    }

    public int GetPretriggerFillLevel()
    {
      // Returns accurate value of the actual pretrigger buffer fill level.
      string jdata = FetchTarget("/pretrigger_buffer_fill_level");

      return (int)JsonConvert.DeserializeObject(jdata);
    }
  }
}