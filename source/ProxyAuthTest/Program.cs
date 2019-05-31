using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Page;
using MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;
using Task = System.Threading.Tasks.Task;
//using System.Diagnostics;
//using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Threading;
using MasterDevs.ChromeDevTools;
using System.IO;
using Chrome = MasterDevs.ChromeDevTools.Protocol.Chrome;

namespace ProxyAuthTest
{
    class Program
    {
        const int ViewPortWidth = 1440;
        const int ViewPortHeight = 900;
        const string proxyUser = "pkdhZA";
        const string proxyPass = "U81kW8";


        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                // STEP 1 - Run Chrome
                var chromeProcessFactory = new ChromeProcessFactory(new StubbornDirectoryCleaner());
                var chromeProcess = chromeProcessFactory.Create(9225, true, "91.215.85.219:8000");
                // STEP 2 - Create a debugging session
                var sessionInfo = (await chromeProcess.GetSessionInfo()).LastOrDefault();
                var chromeSessionFactory = new ChromeSessionFactory();
                IChromeSession chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);

                // STEP 3 - Send a command
                //
                // Here we are sending a commands to tell chrome to set the viewport size 
                // and navigate to the specified URL
                await chromeSession.SendAsync(new SetDeviceMetricsOverrideCommand
                {
                    Width = ViewPortWidth,
                    Height = ViewPortHeight,
                    Scale = 1
                });

                //enable network
                var enableNetwork = await chromeSession.SendAsync(new Chrome.Network.EnableCommand());

                //proxy auth
                ProxyAuthenticate(proxyUser, proxyPass, chromeSession);


                var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
                {
                    Url = "http://mybot.su/login.php"
                });
                await Task.Delay(3000);
                Console.WriteLine("NavigateResponse: " + navigateResponse.Id);


                await Task.Delay(3000);
                chromeProcess.Dispose();
            }).Wait();
        }

        async private static void ProxyAuthenticate(string proxyUser, string proxyPass, IChromeSession chromeSession)
        {
            chromeSession.ProxyAuthenticate(proxyUser, proxyPass);

            await chromeSession.SendAsync(new Chrome.Network.SetCacheDisabledCommand { CacheDisabled = true });

            Chrome.Fetch.RequestPattern[] patterns = { new Chrome.Fetch.RequestPattern { UrlPattern = "*" } };
            await chromeSession.SendAsync(new MasterDevs.ChromeDevTools.Protocol.Chrome.Fetch.EnableCommand { HandleAuthRequests = true, Patterns = patterns });
        }
    }
}
