using LightBuzz.Vitruvius;
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
using System.Runtime.InteropServices;
using System.ComponentModel;

using System.Speech;
using System.Speech.Recognition;
using System.Speech.AudioFormat;


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

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private KinectAudioStream convertStream = null;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;


        /// <summary>
        /// List of all UI span elements used to select recognized text.
        /// </summary>
        private List<Span> recognitionSpans;

        
        public MainWindow()
        {
            InitializeComponent();
        }
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
                Console.WriteLine(recognizers);
            }
            catch (COMException)
            {
                Console.WriteLine("Returning Null");
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                Console.WriteLine("Inside foreach" + recognizer);
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-CA".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Reaced inside");
                    return recognizer;
                }
            }

            return null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
            Console.WriteLine("Window_Loaded");
            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                _gestureController = new GestureController();
                _gestureController.GestureRecognized += GestureController_GestureRecognized;

                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = this._sensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                // create the convert stream
                this.convertStream = new KinectAudioStream(audioStream);
            }
            Console.WriteLine("Before TryGetKinectRecognizer");
            RecognizerInfo ri = TryGetKinectRecognizer();

            Console.WriteLine("After :"+ ri);

            if (null != ri)
            {


                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var directions = new Choices();
                Console.WriteLine("Adding directions");
                directions.Add(new SemanticResultValue("land", "LAND"));
                directions.Add(new SemanticResultValue("stop", "STOP"));


                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);


                this.speechEngine.LoadGrammar(g);


                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                //this.statusBarText.Text = Properties.Resources.NoSpeechRecognizer;
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

            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }
        }

        private void ClearRecognitionHighlights()
        {

        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            // Number of degrees in a right angle.
            const int DegreesInRightAngle = 90;

            // Number of pixels turtle should move forwards or backwards each time.
            const int DisplacementAmount = 60;

            this.ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "LAND":
                        Console.WriteLine("LAND VOICE");
                        break;

                    case "STOP":
                        Console.WriteLine("STOP VOICE");
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.ClearRecognitionHighlights();
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