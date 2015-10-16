using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LightBuzz.Vitruvius;
using WindowsPreview.Kinect;
using System.Windows;
using Microsoft.Kinect;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Text;
using Flurl.Http;



// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace IdentifiedFlyingObject
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private KinectSensor kinectSensor = null;
        static String baseUrl = "http://localhost:8080/api/";
        public MainPage()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();
            // open the sensor
            this.kinectSensor.Open();
            this.InitializeComponent();
           
            //textbox1.Text = "Hii";
        }

        private async void Takeoff_Click(object sender, RoutedEventArgs e)
        {
            String response=null;

            string uriString = "/takeoff";
            try
            {
                response = await httpRequest(uriString);
                textbox1.Text = response;
            }
            catch(Exception)
            {
                textbox1.Text = "Error";
            }
            
        }
        private async void Land_Click(object sender, RoutedEventArgs e)
        {
            String response = null;

            string uriString = "/land";
            try
            {
                response = await httpRequest(uriString);
                textbox1.Text = response;
            }
            catch (Exception)
            {
                textbox1.Text = "Error";
            }
        }
        
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

       
       
    }
}
