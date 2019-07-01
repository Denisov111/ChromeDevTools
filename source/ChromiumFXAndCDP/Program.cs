using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromium;
using Chromium.Remote;
using Chromium.WebBrowser;
using Chromium.Event;
using Chromium.WebBrowser.Event;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;

namespace ChromiumFXAndCDP
{
    static class Program
    {
        static int MinDebugPort = 9222;
        static int MaxDebugPort = 9999;

        static int port = 0;
        static string proxy = null;

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string arguments = "";
            if (args.Length>0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    //Console.WriteLine("args[{0}] == {1}", i, args[i]);
                    arguments += "args["+i+"]="+ args[i]+" ";
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var assemblyDir = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath);

            Environment.CurrentDirectory = Path.Combine(assemblyDir, @"..\..\..\..\");

            if (CfxRuntime.PlatformArch == CfxPlatformArch.x64)
                CfxRuntime.LibCefDirPath = @"cef\Release64";
            else
                CfxRuntime.LibCefDirPath = @"cef\Release";

            ChromiumWebBrowser.OnBeforeCfxInitialize += ChromiumWebBrowser_OnBeforeCfxInitialize;
            ChromiumWebBrowser.OnBeforeCommandLineProcessing += ChromiumWebBrowser_OnBeforeCommandLineProcessing;
            
            ChromiumWebBrowser.Initialize();

            ChromiumWebBrowser wb = new ChromiumWebBrowser();

            wb.RequestHandler.CanSetCookie += (s, e) =>
            {
                e.SetReturnValue(true);
            };

            wb.RequestHandler.CanGetCookies += (s, e) =>
            {
                e.SetReturnValue(true);
            };

            Form1 f = new Form1();
            f.wb = wb;
            f.txt = arguments;
            //f.FormBorderStyle = FormBorderStyle.None;
            f.Size = new System.Drawing.Size(900, 600);


            wb.Dock = DockStyle.Fill;
            wb.Parent = f;
            //wb.LoadUrl("http://mybot.su/ip.php");
            wb.LoadUrl("http://mybot.su");
            //wb.LoadUrl("http://youtube.com");
            Application.Run(f);

            CfxRuntime.Shutdown();
        }

        private static void ChromiumWebBrowser_OnBeforeCfxInitialize(OnBeforeCfxInitializeEventArgs e)
        {
            e.Settings.LocalesDirPath = Path.GetFullPath(@"cef\Resources\locales");
            e.Settings.ResourcesDirPath = Path.GetFullPath(@"cef\Resources");
            e.Settings.RemoteDebuggingPort = GetPort();
            //e.Settings.CachePath = (@"C:\cefcache");
            //e.Settings.PersistSessionCookies = true;
        }

        static void ChromiumWebBrowser_OnBeforeCommandLineProcessing(CfxOnBeforeCommandLineProcessingEventArgs e)
        {
            Console.WriteLine("ChromiumWebBrowser_OnBeforeCommandLineProcessing");
            Console.WriteLine(e.CommandLine.CommandLineString);
            //e.CommandLine.AppendSwitchWithValue("proxy-server", "http://119.191.79.46:80");
            //e.CommandLine.AppendSwitchWithValue("persist-session-cookies", "1");
        }

        public static int GetPort()
        {
            ProcessStartInfo psiOpt = new ProcessStartInfo("cmd.exe", "/C netstat -a -n -o");
            psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
            psiOpt.RedirectStandardOutput = true;
            psiOpt.UseShellExecute = false;
            psiOpt.CreateNoWindow = true;
            // запускаем процесс
            Process procCommand = Process.Start(psiOpt);
            // получаем ответ запущенного процесса
            StreamReader srIncoming = procCommand.StandardOutput;
            // выводим результат
            string[] ss = srIncoming.ReadToEnd().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            StringCollection procStr = new StringCollection();
            procCommand.WaitForExit();

            for (int i = MinDebugPort; i < MaxDebugPort; i++)
            {
                bool portIsBusy = false;
                string portSignature = ":" + i.ToString();
                foreach (string s in ss)
                {
                    if (s.Contains(portSignature))
                    {
                        portIsBusy = true;
                        break;
                    }
                }
                if (portIsBusy)
                {
                    continue;
                }
                else
                {
                    return i;
                }
            }
            return 0;
        }


    }
}
