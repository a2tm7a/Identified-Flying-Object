using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;

namespace VitruviusTest
{
    class MyHttpClient
    {
        String baseUrl = "http://localhost:8080/api/";

        public async Task<string> httpRequest(string url)
        {
            Uri uri = new Uri(baseUrl + url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            string received;

            using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream))
                    {

                        received = await sr.ReadToEndAsync();
                    }
                }
            }

            return received;
        }
        public async void send_request(String url)
        {
            String response = null;

            try
            {
                response = await httpRequest(url);
                Console.WriteLine(response);
            }
            catch (Exception)
            {
                Console.WriteLine(response);
            }
        }
    }
}
