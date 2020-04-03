using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Net.Http;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PhotoStand
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        protected string function_url = "https://coportrait.azurewebsites.net/api/pdraw?code=7mSfb...==";

        MediaCapture MC;
        DispatcherTimer dt = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };

        HttpClient http = new HttpClient();

        FaceDetectionEffect FaceDetector;
        VideoEncodingProperties VideoProps;

        bool IsFacePresent = false;

        ObservableCollection<WriteableBitmap> Faces = new ObservableCollection<WriteableBitmap>();

        int counter;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await Init();
            dt.Tick += CounterCallback;
            dt.Start();
        }

        private async void CounterCallback(object sender, object e)
        {
            counter--;
            Counter.Text = counter.ToString();
            if (counter==0)
            {
                dt.Stop();
                if (DFace == null) return;
                var ms = new MemoryStream();
                await MC.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), ms.AsRandomAccessStream());
                Point p; Size sz;
                ExpandFaceRect1(DFace.FaceBox, out p, out sz);                
                var cb = await CropBitmap.GetCroppedBitmapAsync(ms.AsRandomAccessStream(), p, sz, 1);
                Faces.Add(cb);
                var res = await CallCognitiveFunction(ms);
                ResultImage.Source = new BitmapImage(new Uri(res));
                await WriteableBitmapToStorageFile(cb);
                Counter.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<string> CallCognitiveFunction(MemoryStream ms)
        {
            ms.Position = 0;
            var resp = await http.PostAsync(function_url, new StreamContent(ms));
            return await resp.Content.ReadAsStringAsync();
        }

        private async Task Init()
        {
            MC = new MediaCapture();
            var cameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var camera = cameras.First();
            var settings = new MediaCaptureInitializationSettings() { VideoDeviceId = camera.Id };
            await MC.InitializeAsync(settings);
            ViewFinder.Source = MC;
            
            // Create face detection
            var def = new FaceDetectionEffectDefinition();
            def.SynchronousDetectionEnabled = false;
            def.DetectionMode = FaceDetectionMode.HighPerformance;
            FaceDetector = (FaceDetectionEffect)(await MC.AddVideoEffectAsync(def, MediaStreamType.VideoPreview));
            FaceDetector.FaceDetected += FaceDetectedEvent;
            FaceDetector.DesiredDetectionInterval = TimeSpan.FromMilliseconds(100);
            FaceDetector.Enabled = true;

            await MC.StartPreviewAsync();
            var props = MC.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            VideoProps = props as VideoEncodingProperties;
        }

        private async void FaceDetectedEvent(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => HighlightDetectedFace(args.ResultFrame.DetectedFaces.FirstOrDefault()));
        }

        DetectedFace DFace;

        private async Task HighlightDetectedFace(DetectedFace face)
        {
            var cx = ViewFinder.ActualWidth / VideoProps.Width;
            var cy = ViewFinder.ActualHeight / VideoProps.Height;
            DFace = face;
            if (face==null)
            {
                FaceRect.Visibility = Visibility.Collapsed;
                Counter.Visibility = Visibility.Collapsed;
                dt.Stop();
                IsFacePresent = false;
            }
            else
            {
                // Canvas.SetLeft(FaceRect, face.FaceBox.X);
                // Canvas.SetTop(FaceRect, face.FaceBox.Y);
                FaceRect.Margin = new Thickness(cx*face.FaceBox.X, cy*face.FaceBox.Y, 0, 0);
                FaceRect.Width = cx*face.FaceBox.Width;
                FaceRect.Height = cy*face.FaceBox.Height;
                FaceRect.Visibility = Visibility.Visible;
                Counter.Margin = new Thickness(cx * face.FaceBox.X, cy * face.FaceBox.Y, 0, 0);
                Counter.Width = face.FaceBox.Width;
                if (!IsFacePresent)
                {
                    Counter.Visibility = Visibility.Visible;
                    IsFacePresent = true;
                    counter = 3; Counter.Text = counter.ToString();
                    dt.Start();
                }
            }
        }

        private void ExpandFaceRect1(BitmapBounds r, out Point p, out Size sz)
        {
            var dx = 0.4 * (float)r.Width;
            var dy = 0.5 * (float)r.Height;
            var x = (float)r.X - dx;
            var y = (float)r.Y - dy;
            var h = (float)r.Height + 2 * dy;
            var w = (float)r.Width + 2 * dx;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            p = new Point(x, y);
            sz = new Size(w, h);
        }

        private async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB)
        {
            string FileName = Guid.NewGuid().ToString()+".jpg";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            var file = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFileAsync(FileName, CreationCollisionOption.GenerateUniqueName);
            // var file = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync(FileName, CreationCollisionOption.GenerateUniqueName);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)WB.PixelWidth,
                                    (uint)WB.PixelHeight,
                                    96.0,
                                    96.0,
                                    pixels);
                await encoder.FlushAsync();
            }
            return file;
        }
    }
}
