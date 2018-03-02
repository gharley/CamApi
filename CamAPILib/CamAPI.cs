using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

namespace CamAPILib
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
        private string fetchTarget(string target)
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
        private string postTarget(string target, IDictionary<string, object> data)
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
    }
}