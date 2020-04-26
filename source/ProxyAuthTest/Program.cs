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
using MasterDevs.ChromeDevTools.Protocol.Chrome.Network;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Fetch;

namespace ProxyAuthTest
{
    class Program
    {
        const int ViewPortWidth = 1440;
        const int ViewPortHeight = 900;
        const string proxyUser = "RR3XkJ";
        const string proxyPass = "nEy0TU";


        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                // STEP 1 - Run Chrome
                var chromeProcessFactory = new ChromeProcessFactory(new StubbornDirectoryCleaner());
                //var chromeProcess = chromeProcessFactory.Create(9226, true);
                var chromeProcess = chromeProcessFactory.Create(9401, false, "45.133.32.223:8000", null, "socks5");
                // STEP 2 - Create a debugging session
                var sessionInfo = (await chromeProcess.GetSessionInfo()).LastOrDefault();
                var chromeSessionFactory = new ChromeSessionFactory();
                IChromeSession chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);

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
                await ProxyAuthenticate(proxyUser, proxyPass, chromeSession);


                var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
                {
                    Url = "http://mybot.su/webrtcleak.php"
                });
                await Task.Delay(3000);
                Console.WriteLine("NavigateResponse: " + navigateResponse.Id);


                await Task.Delay(10000);
                Console.ReadKey();
                chromeProcess.Dispose();
            }).Wait();
        }

        private static void AuthRequiredEventHandler(AuthRequiredEvent authRequiredEvent, IChromeSession chromeSession)
        {
            WriteObject(authRequiredEvent);
            string requestId = authRequiredEvent.RequestId;

            Chrome.Fetch.AuthChallengeResponse acr = new Chrome.Fetch.AuthChallengeResponse {
                Response = "ProvideCredentials",
                Username = proxyUser,
                Password = proxyPass
            };

            var auth = chromeSession.SendAsync(new ContinueWithAuthCommand {
                RequestId = requestId,
                AuthChallengeResponse = acr
            });
        }

        private static void RequestPausedEventHandler(RequestPausedEvent requestPausedEvent, IChromeSession chromeSession)
        {
            WriteObject(requestPausedEvent);
            string requestId = requestPausedEvent.RequestId;
            var cont = chromeSession.SendAsync(new Chrome.Fetch.ContinueRequestCommand { RequestId=requestId });
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
            Console.WriteLine("RECIVE <<< "+ob.GetType() + " " + obString);
            if (obString.Contains("94.73.237.177"))
            {
                Console.WriteLine("SEND >>> " + obString);
            }
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
