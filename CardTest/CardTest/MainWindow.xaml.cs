using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PlayingCardRecognition;
using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging.Filters;
using System.IO;


namespace CardTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int CameraWidth = 640;  // constant Width
        private const int CameraHeight = 480; // constant Height

        private FilterInfoCollection cameras; //Collection of Cameras that connected to PC
        private VideoCaptureDevice device; //Current chosen device(camera) 
        private Dictionary<string, string> cameraDict = new Dictionary<string, string>();
        private System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Brushes.Orange, 4); //is used for drawing rectangle around card
        private Font font = new Font("Tahoma", 15, System.Drawing.FontStyle.Bold); //is used for writing string on card
        private CardRecognizer recognizer = new CardRecognizer();
        private CardCollection cards;
        private int frameCounter = 0;

        public MainWindow()
        {

          InitializeComponent();

          //Fetch cameras 
          this.cameras = new FilterInfoCollection(AForge.Video.DirectShow.FilterCategory.VideoInputDevice);
          int i = 1;
          foreach (AForge.Video.DirectShow.FilterInfo camera in this.cameras)
          {
              if (!this.cameraDict.ContainsKey(camera.Name))
                  this.cameraDict.Add(camera.Name, camera.MonikerString);
              else
              {
                  this.cameraDict.Add(camera.Name + "-" + i.ToString(), camera.MonikerString);
                  i++;
              }
          }
          this.cbCamera.ItemsSource = new List<string>(cameraDict.Keys); //Bind camera names to combobox

          if (this.cbCamera.Items.Count == 0)
              button1.IsEnabled = false;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (button1.Content == "Start")
            {
                this.button1.Content = "Stop";
                this.device = new VideoCaptureDevice(this.cameraDict[cbCamera.SelectedItem.ToString()]);
                this.device.NewFrame += new NewFrameEventHandler(videoNewFrame);
                this.device.DesiredFrameSize = new System.Drawing.Size(CameraWidth, CameraHeight);

                device.Start(); //Start Device
            }
            else
            {
                this.StopCamera();
                button1.Content = "Start";
                this.pictureBox1.Source = null;
            }


        }

        private Bitmap ResizeBitmap(Bitmap bmp)
        {
            ResizeBilinear resizer = new ResizeBilinear((int)pictureBox1.Width, (int)pictureBox1.Height);

            return resizer.Apply(bmp);
        }

        private void videoNewFrame(object sender, NewFrameEventArgs args)
        {

            Bitmap temp = args.Frame.Clone() as Bitmap;

            try
            {
                frameCounter++;

                if (frameCounter > 10)
                {
                    cards = recognizer.Recognize(temp);
                    frameCounter = 0;
                }

                //Draw Rectangle around cards and write card strings on card
                using (Graphics graph = Graphics.FromImage(temp))
                {
                    foreach (Card card in cards)
                    {
                        graph.DrawPolygon(pen, card.Corners); //Draw a polygon around card
                        PointF point = CardRecognizer.GetStringPoint(card.Corners); //Find Top left corner
                        point.Y += 10;
                        graph.DrawString(card.ToString(), font, System.Drawing.Brushes.White, point); //Write string on card
                    }
                }
            }
            catch { }
            this.pictureBox1.Source = MakeReadyForWPF(  ResizeBitmap(temp));
        }
        public static BitmapImage MakeReadyForWPF(Bitmap pic)
        {
            Bitmap image = pic;

            System.Drawing.Bitmap dImg = image;
            MemoryStream ms = new MemoryStream();
            dImg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            System.Windows.Media.Imaging.BitmapImage bImg = new System.Windows.Media.Imaging.BitmapImage();
            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(ms.ToArray());
            bImg.EndInit();
            return bImg;
        }


        private void StopCamera()
        {
            if (device != null && device.IsRunning)
            {
                device.SignalToStop(); //stop device
                device.WaitForStop();
                device = null;
            }
        }
    }
}
