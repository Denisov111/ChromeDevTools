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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        enum ShowWindowCommands
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window 
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
                          /// <summary>
                          /// Activates the window and displays it as a maximized window.
                          /// </summary>       
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value 
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position. 
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level 
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is 
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position. 
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the 
            /// STARTUPINFO structure passed to the CreateProcess function by the 
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
            /// that owns the window is not responding. This flag should only be 
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }
        

        const string proxyUser = "pkdhZA";
        const string proxyPass = "U81kW8";

        private Process pDocked;
        private IntPtr hWndOriginalParent;
        private IntPtr hWndDocked;
        public System.Windows.Forms.Panel pannel;

        IChromeSession chromeSession;

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
            var chromeProcess = chromeProcessFactory.Create(9238, false, "91.215.85.219:8000");
            Process pr = ((RemoteChromeProcess)chromeProcess).Process;
            // STEP 2 - Create a debugging session
            var sessionInfo = (await chromeProcess.GetSessionInfo()).LastOrDefault();
            var chromeSessionFactory = new ChromeSessionFactory();
            chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);

            //Process pr = Process.Start("notepad.exe");

            if (hWndDocked != IntPtr.Zero) //don't do anything if there's already a window docked.
                return;

            var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
            {
                Url = "about:blank"
            });


            List < IntPtr> childHandles=null;
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
            //hWndOriginalParent = SetParent(childHandles[1], pannel.Handle);
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
            
            //var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
            //{
            //    Url = "https://google.com"
            //});
            //await Task.Delay(3000);
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
            var res = MoveWindow(hWndDocked, -10, -80, pannel.Width+10, pannel.Height+75, true);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AlignToPannel();
            //hWndDocked = pDocked.MainWindowHandle;  //cache the window handle
            //List<IntPtr> childHandles = GetAllChildHandles(hWndDocked);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ShowWindow(hWndDocked, ShowWindowCommands.Maximize);

            //SetForegroundWindow(hWndDocked);
            //ShowWindow(hWndDocked, (ShowWindowCommands)SW_SHOWNORMAL);
            //AlignToPannel();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ShowWindow(hWndDocked, ShowWindowCommands.Normal);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ShowWindow(hWndDocked, ShowWindowCommands.ShowMinimized);
        }

        async private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
            {
                Url = "https://google.com"
            });
        }

        async private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            
            ShowWindow(hWndDocked, ShowWindowCommands.Minimize);
            await Task.Delay(30);
            ShowWindow(hWndDocked, ShowWindowCommands.Normal);
            await Task.Delay(100);
            AlignToPannel();
        }
    }
}
