﻿using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace VitruviusTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Visualization _mode = Visualization.Color;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IEnumerable<Body> _bodies;
        GestureController _gestureController;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                _gestureController = new GestureController();
                _gestureController.GestureRecognized += GestureController_GestureRecognized;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_sensor != null)
            {
                _sensor.Close();
                _sensor = null;
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Visualization.Color)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Visualization.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Visualization.Infrared)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    tblHeights.Text = string.Empty;

                    _bodies = frame.Bodies().Where(body => body.IsTracked);

                    foreach (var body in _bodies)
                    {
                        if (body.IsTracked)
                        {
                            // Update body gestures.
                            _gestureController.Update(body);

                            // Draw body.
                            //canvas.Source = body.ToBitmap(_mode);

                            // Display user height.
                            tblHeights.Text += string.Format("\nUser {0}: {1}cm", body.TrackingId, Math.Round(body.Height(), 2));
                        }
                    }
                }
            }
        }

        void GestureController_GestureRecognized(object sender, GestureEventArgs e)
        {
            MyHttpClient request = new MyHttpClient();
           
            //Do something according to the type of the gesture.
            switch (e.GestureType)
            {

                case GestureType.JoinedHands:
                    Console.WriteLine("JH");
                    request.send_request("/takeoff");
                    Console.WriteLine("Aaye aaye! Captain.");
                    break;
                case GestureType.Menu:
                    Console.WriteLine("Menu");
                    break;
                case GestureType.SwipeDown:
                    Console.WriteLine("SD");
                    break;
                case GestureType.SwipeLeft:
                    Console.WriteLine("Sl");
                    break;
                case GestureType.SwipeRight:
                    Console.WriteLine("SR");
                    break;
                case GestureType.SwipeUp:
                    Console.WriteLine("SU");
                    break;
                case GestureType.WaveLeft:
                    Console.WriteLine("WL");
                    break;
                case GestureType.WaveRight:
                    Console.WriteLine("WR");
                    request.send_request("/land");
                    Console.WriteLine("Au Revoir");
                    break;
                case GestureType.ZoomIn:
                    Console.WriteLine("ZI");
                    break;
                case GestureType.ZoomOut:
                    Console.WriteLine("ZO");
                    break;
                default:
                    break;
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            _mode = Visualization.Color;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Visualization.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Visualization.Infrared;
        }
    }
}