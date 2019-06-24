using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Page;
using MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;
using Task = System.Threading.Tasks.Task;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using MasterDevs.ChromeDevTools;
using System.IO;
using Chrome = MasterDevs.ChromeDevTools.Protocol.Chrome;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Network;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Fetch;

namespace WPFEmbededTest
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        private IntPtr _MainHandle;


        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        const string proxyUser = "pkdhZA";
        const string proxyPass = "U81kW8";

        private Process pDocked;
        private IntPtr hWndOriginalParent;
        private IntPtr hWndDocked;
        public System.Windows.Forms.Panel pannel;

        public MainWindow()
        {
            InitializeComponent();
            pannel = new System.Windows.Forms.Panel();
            host.Child = pannel;
            dockIt();
        }

        async private void dockIt()
        {
            // STEP 1 - Run Chrome
            var chromeProcessFactory = new ChromeProcessFactory(new StubbornDirectoryCleaner());
            var chromeProcess = chromeProcessFactory.Create(9236, false, "91.215.85.219:8000");
            Process pr = ((RemoteChromeProcess)chromeProcess).Process;
            // STEP 2 - Create a debugging session
            var sessionInfo = (await chromeProcess.GetSessionInfo()).LastOrDefault();
            var chromeSessionFactory = new ChromeSessionFactory();
            IChromeSession chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);
            
            if (hWndDocked != IntPtr.Zero) //don't do anything if there's already a window docked.
                return;


            List<IntPtr> childHandles=null;
            pDocked = pr;
            while (hWndDocked == IntPtr.Zero)
            {
                pDocked.WaitForInputIdle(1000); //wait for the window to be ready for input;
                pDocked.Refresh();              //update process info
                if (pDocked.HasExited)
                {
                    return; //abort if the process finished before we got a handle.
                }
                hWndDocked = pDocked.MainWindowHandle;  //cache the window handle
                childHandles = GetAllChildHandles(hWndDocked);
            }
            //Windows API call to change the parent of the target window.
            //It returns the hWnd of the window's parent prior to this call.
            hWndOriginalParent = SetParent(hWndDocked, pannel.Handle);
            //hWndOriginalParent = SetParent(childHandles[0], pannel.Handle);
            //Wire up the event to keep the window sized to match the control
            SizeChanged += window_SizeChanged;
            //Perform an initial call to set the size.
            AlignToPannel();


            chromeSession.Subscribe<DataReceivedEvent>(dataReceivedEvent =>
            {
                DataReceivedEventHandler(dataReceivedEvent);
            });

            chromeSession.Subscribe<EventSourceMessageReceivedEvent>(eventSourceMessageReceivedEvent =>
            {
                EventSourceMessageReceivedEventHandler(eventSourceMessageReceivedEvent);
            });

            chromeSession.Subscribe<RequestWillBeSentEvent>(requestWillBeSentEvent =>
            {
                RequestWillBeSentEventHandler(requestWillBeSentEvent);
            });

            chromeSession.Subscribe<RequestPausedEvent>(requestPausedEvent =>
            {
                RequestPausedEventHandler(requestPausedEvent, chromeSession);
            });

            chromeSession.Subscribe<AuthRequiredEvent>(authRequiredEvent =>
            {
                AuthRequiredEventHandler(authRequiredEvent, chromeSession);
            });

            //enable network
            var enableNetwork = await chromeSession.SendAsync(new Chrome.Network.EnableCommand());

            //proxy auth
            await ProxyAuthenticate(proxyUser, proxyPass, chromeSession);

            await Task.Delay(1000);
            var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
            {
                Url = "https://google.com"
            });
            await Task.Delay(3000);
        }

        public List<IntPtr> GetAllChildHandles(IntPtr hWndDockedChome)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(hWndDockedChome, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }

        private void AlignToPannel()
        {
            MoveWindow(hWndDocked, -10, -10, pannel.Width+10, pannel.Height+80, true);
        }

        void window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AlignToPannel();
        }

        private static void AuthRequiredEventHandler(AuthRequiredEvent authRequiredEvent, IChromeSession chromeSession)
        {
            WriteObject(authRequiredEvent);
            string requestId = authRequiredEvent.RequestId;

            Chrome.Fetch.AuthChallengeResponse acr = new Chrome.Fetch.AuthChallengeResponse
            {
                Response = "ProvideCredentials",
                Username = proxyUser,
                Password = proxyPass
            };

            var auth = chromeSession.SendAsync(new ContinueWithAuthCommand
            {
                RequestId = requestId,
                AuthChallengeResponse = acr
            });
        }

        private static void RequestPausedEventHandler(RequestPausedEvent requestPausedEvent, IChromeSession chromeSession)
        {
            WriteObject(requestPausedEvent);
            string requestId = requestPausedEvent.RequestId;
            var cont = chromeSession.SendAsync(new Chrome.Fetch.ContinueRequestCommand { RequestId = requestId });
        }



        private static void RequestWillBeSentEventHandler(RequestWillBeSentEvent requestWillBeSentEvent)
        {
            WriteObject(requestWillBeSentEvent);
        }

        private static void EventSourceMessageReceivedEventHandler(EventSourceMessageReceivedEvent eventSourceMessageReceivedEvent)
        {
            WriteObject(eventSourceMessageReceivedEvent);
        }

        private static void DataReceivedEventHandler(DataReceivedEvent dataReceivedEvent)
        {
            WriteObject(dataReceivedEvent);
        }

        private static void WriteObject(Object ob)
        {
            string obString = Newtonsoft.Json.JsonConvert.SerializeObject(ob);
            Console.WriteLine("RECIVE <<< " + ob.GetType() + " " + obString);
        }

        async private static Task ProxyAuthenticate(string proxyUser, string proxyPass, IChromeSession chromeSession)
        {
            chromeSession.ProxyAuthenticate(proxyUser, proxyPass);

            await chromeSession.SendAsync(new Chrome.Network.SetCacheDisabledCommand { CacheDisabled = true });

            Chrome.Fetch.RequestPattern[] patterns = { new Chrome.Fetch.RequestPattern { UrlPattern = "*" } };
            await chromeSession.SendAsync(new Chrome.Fetch.EnableCommand { HandleAuthRequests = true, Patterns = patterns });
        }
    }
}
