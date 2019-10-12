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
using System.Collections.ObjectModel;

namespace WPFEmbededTest
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window

    {
        const int ViewPortWidth = 1440;
        const int ViewPortHeight = 900;
        const string proxyUser = "KE3jnd";
        const string proxyPass = "yfFXNU";

        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);

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

        private Process pDocked;
        private IntPtr hWndOriginalParent;
        private IntPtr hWndDocked;
        public System.Windows.Forms.Panel pannel;

        IChromeSession chromeSession;
        List<IntPtr> childHandles;
        int procId;

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
            //var chromeProcess = chromeProcessFactory.Create(9401, false, "193.31.103.236:9397");
            var chromeProcess = chromeProcessFactory.Create(9504, false, null, "jniherujherfjio");
            //var chromeProcess1 = chromeProcessFactory.Create(9504, false);
            Process pr = ((RemoteChromeProcess)chromeProcess).Process;
            // STEP 2 - Create a debugging session
            var sessionInfo = (await chromeProcess.GetSessionInfo()).LastOrDefault();
            var chromeSessionFactory = new ChromeSessionFactory();
            chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);

            //Process pr = Process.Start("notepad.exe");

            if (hWndDocked != IntPtr.Zero) //don't do anything if there's already a window docked.
                return;

            try
            {
                var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
                {
                    Url = "https://google.com"
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            


            childHandles=null;
            pDocked = pr;
            procId = pr.Id;
            while (hWndDocked == IntPtr.Zero)
            {
                pDocked.WaitForInputIdle(1000); //wait for the window to be ready for input;
                pDocked.Refresh();              //update process info
                if (pDocked.HasExited)
                {
                    return; //abort if the process finished before we got a handle.
                }
                hWndDocked = pDocked.MainWindowHandle;  //cache the window handle
                //var proceeses=pDocked.
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
            
            //enable network
            var enableNetwork = await chromeSession.SendAsync(new Chrome.Network.EnableCommand());
            

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

        async private void AlignToPannel()
        {
            pannel.Width = (int)host.Width;
            pannel.Height = (int)host.Height;
            var res = MoveWindow(hWndDocked, -10, -10, pannel.Width+10, pannel.Height+75, true);
            //int WM_PAINT = 0xF;
            //await Task.Delay(1000);
            //SendMessage(childHandles[0], WM_PAINT, IntPtr.Zero, IntPtr.Zero);
            await Task.Delay(100);
            InvalidateRect(childHandles[0], IntPtr.Zero, true);
            UpdateWindow(childHandles[0]);
            //await Task.Delay(100);
            //InvalidateRect(hWndDocked, IntPtr.Zero, true);
            //UpdateWindow(hWndDocked);
        }

        void window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AlignToPannel();
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AlignToPannel();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ShowWindow(hWndDocked, ShowWindowCommands.Maximize);
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
            //await Task.Delay(30);
            ShowWindow(hWndDocked, ShowWindowCommands.Normal);
            //await Task.Delay(100);
            AlignToPannel();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = FindWindow("Chrome_RenderWidgetHostHWND", null);
            IntPtr hWnd2 = FindWindow(null, "Chrome_RenderWidgetHostHWND");
            findWindow.Content = hWnd.ToString()+" "+ hWnd2.ToString();

            childHandles = GetAllChildHandles(hWndDocked);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            SetActiveWindow(childHandles[0]);
        }


        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        async private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            //var res = chromeSession.SendAsync<GetProcessInfoCommand>().Result;

            var hwnds = EnumerateProcessWindowHandles(procId);
            var windInfos = GetInfoAboutWindows(hwnds);


        }

        private object GetInfoAboutWindows(IEnumerable<IntPtr> hwnds)
        {
            ObservableCollection<HwndInfo> coll = new ObservableCollection<HwndInfo>();

            foreach(IntPtr intPtr in hwnds)
            {
                string title = GetText(intPtr);
                StringBuilder className = new StringBuilder(256);
                GetClassName(intPtr, className, className.Capacity);
                coll.Add(new HwndInfo() {
                    Title =title,
                    Hwnd=intPtr,
                    ClassName=className.ToString()
                });
            }


            

            return coll;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }

        public static string GetText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);
    }
}
