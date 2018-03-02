using System;
using System.Collections;
using System.Net;

namespace CamAPIC_
{
  public class CamAPI
  {
    private string camAddr = null;

    public CamAPI(string address)
    {
      camAddr = address;

      Console.WriteLine(string.Format("CAMAPI HTTP initialized.  Talking to camera: {0}", this.camAddr));
    }

    // Returns data fetched from the target URL or None if HTTP returns an error trying to fetch the URL
    private string fetchTarget(string target){
      string data = null;
      string url = "http://" + this.camAddr + target;

      try{
        Console.WriteLine(string.Format("    Fetching: {0}", url));

        var request = WebRequest.Create(url);
        var response = request.GetResponse();

        data = response.ToString();
      }catch(Exception ex){
        Console.WriteLine("ERROR: unable to fetch: " + url);
      }

      return data;
    }

    private string postTarget(string url, IDictionary data){
    return "";
    }
  }
}