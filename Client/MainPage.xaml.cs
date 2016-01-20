using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BitmapImage img = new BitmapImage();

        private MemoryStream ms = new MemoryStream();

        private AgentConnection agentConnection = new AgentConnection();

        private bool continueConfiguration = true;

        public object Thread { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            this.agentConnection.OnDataReceived += Agent_OnDataReceived;
            this.agentConnection.OnSlideEnded += AgentConnection_OnSlideEnded;
            this.agentConnection.OnConnectionFailed += AgentConnection_OnConnectionFailed;

            var webpage1 = new Webpage("http://www.google.at");
            var webpage2 = new Webpage("http://www.youtube.com");

            var screen1 = new ScreenCapture("10.101.150.13", "firefox");
            var screen2 = new ScreenCapture("10.101.150.13", "notepad++");

            var slide1 = new Slide(1000, webpage1);
            var slide2 = new Slide(10000, screen1);
            var slide3 = new Slide(3000, webpage2);
            var slide4 = new Slide(10000, screen2);

            var config = new Configuration(new List<Slide>() { slide1,slide2,slide3,slide4 });

            Task.Run(() => { this.ConfigurationWorker(config); });

            //StartVideo();
        }

        private async void AgentConnection_OnConnectionFailed(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                if (web.Visibility == Visibility.Visible)
                {
                    web.Visibility = Visibility.Collapsed;
                }
                var srce = new BitmapImage(new Uri(this.BaseUri,"/Assets/Fail.bmp"));
                pic.Source = srce;
                pic.Visibility = Visibility.Visible;
            }
            );
        }

        private void AgentConnection_OnSlideEnded(object sender, EventArgs e)
        {
            this.continueConfiguration = true;
        }

        private async void ConfigurationWorker(Configuration config)
        {
            while (true)
            {
                foreach(var slide in config.Diashow)
                {
                    if(slide.Content is ScreenCapture)
                    {
                        this.agentConnection.ConnectTo(slide);
                    }
                    else if(slide.Content is Webpage)
                    {
                        var webpage = slide.Content as Webpage;
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,() =>
                        {
                            web.Source = new Uri(webpage.URL);
                            web.LoadCompleted += Web_LoadCompleted;
                        });
                    }
                    await Task.Delay(slide.AppearanceDuration);
                }   
            }
        }

        private void Web_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (pic.Visibility == Visibility.Visible)
            {
                pic.Visibility = Visibility.Collapsed;
            }
            web.Visibility = Visibility.Visible;
        }

        private void InitializingClient()
        {
            var conDirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            conDirInfo = new DirectoryInfo(Path.Combine(conDirInfo.FullName, "Configuration"));
            if (conDirInfo.Exists)
            {
                var conFileInfo = new FileInfo("Configuration\\recad.conf");
                if (conFileInfo != null)
                {
                    try
                    {
                        var confFile = File.ReadAllLines(Path.Combine("Configuration", "recad.conf"));
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText("errorLog.txt", "##BEGINERROR-- " + ex.Message + " --END-ERROR##");
                    }
                }
            }
            else
            {
                try
                {
                    conDirInfo.Create();
                }
                catch(Exception ex)
                {
                    File.WriteAllText("errorLog.txt", "##BEGINERROR-- " + ex.Message + " --END-ERROR##");
                }
            }
        }

        private async void Agent_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                {
                    this.ms.Seek(0, SeekOrigin.Begin);
                    this.ms.Write(e.Buffer, 0, e.Buffer.Length);
                    this.ms.Seek(0, SeekOrigin.Begin);
                    img.SetSource(ms.AsRandomAccessStream());
                    pic.Source = img;
                    if(web.Visibility == Visibility.Visible)
                    {
                        web.Visibility = Visibility.Collapsed;
                    }
                    pic.Visibility = Visibility.Visible;
                }
            );
        }
    }
}
