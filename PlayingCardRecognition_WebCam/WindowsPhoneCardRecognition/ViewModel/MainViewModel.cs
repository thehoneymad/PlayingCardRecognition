using GalaSoft.MvvmLight;
using Lumia.Imaging;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System;
using Windows.UI.Core;
using System.Collections.Generic;
using Lumia.Imaging.Artistic;
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using Lumia.Imaging.Adjustments;

namespace WindowsPhoneCardRecognition.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
       

        private string _status;
        private CameraPreviewImageSource _cameraPreviewImageSource; // Using camera as our image source
        private WriteableBitmap _writeableBitmap; // Target for our renderer
        
        private WriteableBitmapRenderer _writeableBitmapRenderer; // renderer for our images
        private bool _isRendering; // Used to prevent multiple renderers running at once
        

        private bool _initialized;

        

        public string Status
        {
            get
            {
                return _status;
            }

            private set
            {
                if (_status != value)
                {
                    _status = value;

                    RaisePropertyChanged("Status");
                }
            }
        }

        

        public bool Initialized
        {
            get
            {
                return _initialized;
            }

            private set
            {
                if (_initialized != value)
                {
                    _initialized = value;

                    RaisePropertyChanged("Initialized");
                }
            }
        }

        public WriteableBitmap PreviewBitmap
        {
            get
            {
                return _writeableBitmap;
            }

            private set
            {
                if (_writeableBitmap != value)
                {
                    _writeableBitmap = value;

                    RaisePropertyChanged("PreviewBitmap");
                }
            }
        }



        public MainViewModel()
        {
            
        }

        /// <summary>
        /// Initialize and start the camera preview
        /// </summary>
        public async Task InitializeAsync()
        {
            Status = "Starting camera...";

            // Create a camera preview image source (from Imaging SDK)
            _cameraPreviewImageSource = new CameraPreviewImageSource();
            await _cameraPreviewImageSource.InitializeAsync(string.Empty);
            var properties = await _cameraPreviewImageSource.StartPreviewAsync();

            // Create a preview bitmap with the correct aspect ratio
            var width = 640.0;
            var height = (width / properties.Width) * properties.Height;
            var bitmap = new WriteableBitmap((int)width, (int)height);

            PreviewBitmap = bitmap;

            // Create a filter effect to be used with the source (no filters yet)
            
            _writeableBitmapRenderer = new WriteableBitmapRenderer(_cameraPreviewImageSource, _writeableBitmap);

            // Attach preview frame delegate
            _cameraPreviewImageSource.PreviewFrameAvailable += OnPreviewFrameAvailable;

            Status = "Initialized";

            Initialized = true;
            
        }

        public async Task PausePreviewAsync()
        {
            if (Initialized)
            {
                await _cameraPreviewImageSource.StopPreviewAsync();
            }
        }

        public async Task ResumePreviewAsync()
        {
            if (Initialized)
            {
                await _cameraPreviewImageSource.InitializeAsync(string.Empty);
                await _cameraPreviewImageSource.StartPreviewAsync();
            }
        }

        private int frameCounter = 0;
        private CardRecognizer recognizer = new CardRecognizer();
        private List<Card> cards = new List<Card>();


        /// <summary>
        /// Render a frame with the selected filter
        /// </summary>
        private async void OnPreviewFrameAvailable(IImageSize args)
        {
            // Prevent multiple rendering attempts at once
            if (Initialized && !_isRendering)
            {
                _isRendering = true;

                // User changed the filter, let's update it before rendering


                // Render the image with the filter
                await _writeableBitmapRenderer.RenderAsync();

                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                        {
                            Status = "Trying to do something .. ";

                            System.Drawing.Bitmap temp = (System.Drawing.Bitmap)_writeableBitmap;

                            try
                            {
                                frameCounter++;

                                if(frameCounter>10)
                                {
                                    cards = recognizer.Recognize(temp);
                                    frameCounter = 0;
                                }
                            }
                            catch
                            { }

                            _writeableBitmap.Invalidate();
                        });

                _isRendering = false;

                Status = "Done Something";
            }
        }
    }
}