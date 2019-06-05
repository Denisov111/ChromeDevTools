using MasterDevs.ChromeDevTools.Protocol.Chrome.Page;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;
//using MasterDevs.ChromeDevTools.Sample;
using Task = System.Threading.Tasks.Task;
//using System.Diagnostics;
//using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Xml.Linq;


namespace MasterDevs.ChromeDevTools.Sample
{
    internal class Program
    {
        const int ViewPortWidth = 1440;
        const int ViewPortHeight = 900;
        private static void Main(string[] args)
        {
            // synchronization
            var screenshotDone = new ManualResetEventSlim();

            // STEP 1 - Run Chrome
            var chromeProcessFactory = new ChromeProcessFactory(new StubbornDirectoryCleaner());
            var chromeProcess = chromeProcessFactory.Create(9397, false);

            var sessionInfo = chromeProcess.GetSessionInfo().Result.LastOrDefault();
            var chromeSessionFactory = new ChromeSessionFactory();
            IChromeSession chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);

            Task.Run(async () =>
            {
                /*
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
                foreach (string s in ss)
                {
                    if (s.Contains("9222"))
                    {

                        string[] parts = s.Split(null);
                        int lenght = parts.Length;
                        int pid = Int32.Parse(parts[lenght - 1]);
                        var proc = Process.GetProcessById(pid);
                        //proc.Kill();
                        procStr.Add(s);
                    }


                }

                Console.WriteLine(srIncoming.ReadToEnd());
                // закрываем процесс
                procCommand.WaitForExit();*/



                    // STEP 2 - Create a debugging session
                    

                    //cookies
                    var ccs = await chromeSession.SendAsync(new Protocol.Chrome.Network.GetAllCookiesCommand());
                    //await chromeSession.SendAsync(new Protocol.Chrome.Network.ClearBrowserCookiesCommand());

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
                    var enableNetwork = await chromeSession.SendAsync(new Protocol.Chrome.Network.EnableCommand());

                    var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
                    {
                        Url = "http://mybot.su/login.php"
                    });
                    await Task.Delay(10000);
                    Console.WriteLine("NavigateResponse: " + navigateResponse.Id);

                    // STEP 4 - Register for events (in this case, "Page" domain events)
                    // send an command to tell chrome to send us all Page events
                    // but we only subscribe to certain events in this session
                    var pageEnableResult = await chromeSession.SendAsync<Protocol.Chrome.Page.EnableCommand>();
                    Console.WriteLine("PageEnable: " + pageEnableResult.Id);

                    Console.WriteLine("Taking screenshot");
                    var screenshot = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });

                    var data = Convert.FromBase64String(screenshot.Result.Data);
                    File.WriteAllBytes("output.png", data);
                    Console.WriteLine("Screenshot stored");
                    /*
                    chromeSession.Subscribe<LoadEventFiredEvent>(loadEventFired =>
                    {
                        // we cannot block in event handler, hence the task
                        Task.Run(async () =>
                        {
                            Console.WriteLine("LoadEventFiredEvent: " + loadEventFired.Timestamp);

                            var documentNodeId = (await chromeSession.SendAsync(new GetDocumentCommand())).Result.Root.NodeId;
                            var bodyNodeId =
                                (await chromeSession.SendAsync(new QuerySelectorCommand
                                {
                                    NodeId = documentNodeId,
                                    Selector = "body"
                                })).Result.NodeId;
                            var height = (await chromeSession.SendAsync(new GetBoxModelCommand { NodeId = bodyNodeId })).Result.Model.Height;

                            await chromeSession.SendAsync(new SetDeviceMetricsOverrideCommand
                            {
                                Width = ViewPortWidth,
                                Height = height,
                                Scale = 1
                            });

                            await Task.Delay(1000);

                            Console.WriteLine("Taking screenshot");
                            var screenshot = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });

                            var data = Convert.FromBase64String(screenshot.Result.Data);
                            File.WriteAllBytes("output.png", data);
                            Console.WriteLine("Screenshot stored");

                            // tell the main thread we are done
                            screenshotDone.Set();
                        });
                    });*/

                    // wait for screenshoting thread to (start and) finish
                    //screenshotDone.Wait();

                    ICommandResponse cr = chromeSession.SendAsync<GetDocumentCommand>().Result;
                    long docNodeId = ((CommandResponse<GetDocumentCommandResponse>)cr).Result.Root.NodeId;
                    //var dom = Protocol.Chrome.DOM.GetDocumentCommand();

                    var qs = chromeSession.SendAsync(new QuerySelectorCommand
                    {
                        NodeId = docNodeId,
                        Selector = "#login_input_username"
                    }).Result;
                    var emailNodeId = qs.Result.NodeId;

                    var sv = chromeSession.SendAsync(new FocusCommand
                    {
                        NodeId = emailNodeId
                    });

                    var it = chromeSession.SendAsync(new ChromeDevTools.Protocol.Chrome.Input.InsertTextCommand
                    {
                        Text = "example@mail.ru"
                    });

                    //pwd
                    var qsPwd = chromeSession.SendAsync(new QuerySelectorCommand
                    {
                        NodeId = docNodeId,
                        Selector = "#login_input_password"
                    }).Result;
                    var pwdNodeId = qsPwd.Result.NodeId;

                    var svPwd = chromeSession.SendAsync(new FocusCommand
                    {
                        NodeId = pwdNodeId
                    });

                    var itPwd = chromeSession.SendAsync(new ChromeDevTools.Protocol.Chrome.Input.InsertTextCommand
                    {
                        Text = "qwerty"
                    });



                    await Task.Delay(1000);

                    Console.WriteLine("Taking screenshot 2");
                    var screenshot2 = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });

                    var data2 = Convert.FromBase64String(screenshot2.Result.Data);
                    File.WriteAllBytes("output2.png", data2);
                    Console.WriteLine("Screenshot 2 stored");


                    //click button
                    var qsButton = chromeSession.SendAsync(new QuerySelectorCommand
                    {
                        NodeId = docNodeId,
                        Selector = "body:nth-child(2) div.container:nth-child(1) form:nth-child(21) > button.btn.btn-default:nth-child(4)"
                    }).Result;
                    var buttonNodeId = qsButton.Result.NodeId;
                    Console.WriteLine("buttonNodeId " + buttonNodeId);

                ///take screenshot button
                ///


                    var buttonBox = chromeSession.SendAsync(new GetBoxModelCommand
                    {
                        NodeId = buttonNodeId
                    });
                    var buttonBoxRes = buttonBox.Result.Result.Model;

                    double leftBegin = buttonBoxRes.Border[0];
                    double leftEnd = buttonBoxRes.Border[2];
                    double topBegin = buttonBoxRes.Border[1];
                    double topEnd = buttonBoxRes.Border[5];

                    double x = Math.Round((leftBegin + leftEnd) / 2, 2);
                    double y = Math.Round((topBegin + topEnd) / 2, 2);

                    try
                    {
                        var click = chromeSession.SendAsync(new ChromeDevTools.Protocol.Chrome.Input.DispatchMouseEventCommand
                        {
                            Type = "mousePressed",
                            X = x,
                            Y = y,
                            ClickCount = 1,
                            Button = "left"
                        });

                        await Task.Delay(100);

                        var clickReleased = chromeSession.SendAsync(new ChromeDevTools.Protocol.Chrome.Input.DispatchMouseEventCommand
                        {
                            Type = "mouseReleased",
                            X = x,
                            Y = y,
                            ClickCount = 1,
                            Button = "left"
                        });

                        Console.WriteLine("midle " + x + " " + y);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    
                    await Task.Delay(5000);

                    await chromeSession.SendAsync(new SetDeviceMetricsOverrideCommand
                    {
                        Width = ViewPortWidth,
                        Height = ViewPortHeight,
                        Scale = 1
                    });

                    Console.WriteLine("Taking screenshot 3");
                    var screenshot3 = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });

                    var data3 = Convert.FromBase64String(screenshot3.Result.Data);
                    File.WriteAllBytes("output3.png", data3);
                    Console.WriteLine("Screenshot 3 stored");


                    //scrolling
                    /*
                    ICommandResponse cr_ = chromeSession.SendAsync<GetDocumentCommand>().Result;
                    long docNodeId2 = ((CommandResponse<GetDocumentCommandResponse>)cr_).Result.Root.NodeId;

                    var bodyNodeId2 =
                                (await chromeSession.SendAsync(new QuerySelectorCommand
                                {
                                    NodeId = docNodeId2,
                                    Selector = "body"
                                })).Result.NodeId;

                    var fc = chromeSession.SendAsync(new FocusCommand
                    {
                        NodeId = bodyNodeId2
                    });*/
                    

                    await Task.Delay(100);

                    var scroll = chromeSession.SendAsync(new ChromeDevTools.Protocol.Chrome.Input.DispatchMouseEventCommand
                    {
                        Type = "mouseWheel",
                        X = x,
                        Y = y,
                        DeltaX=0,
                        DeltaY=10000
                    });

                    await Task.Delay(1000);

                    Console.WriteLine("Taking screenshot 4");
                    var screenshot4 = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });
                    var data4 = Convert.FromBase64String(screenshot4.Result.Data);
                    File.WriteAllBytes("output4.png", data4);
                    Console.WriteLine("Screenshot 4 stored");


                    //cookies
                    var cookies = (await chromeSession.SendAsync(new Protocol.Chrome.Network.GetAllCookiesCommand())).Result.Cookies;


                    //cookies to json string format
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(cookies);


                    //delete cookies
                    await chromeSession.SendAsync(new Protocol.Chrome.Network.ClearBrowserCookiesCommand());


                    //check delete cookies
                    var cookiesForCheck = (await chromeSession.SendAsync(new Protocol.Chrome.Network.GetAllCookiesCommand())).Result.Cookies;
                    var navigateResponse2 = await chromeSession.SendAsync(new NavigateCommand
                    {
                        Url = "http://mybot.su/login.php"
                    });

                    Console.WriteLine("Taking screenshot 5");
                    var screenshot5 = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });
                    var data5 = Convert.FromBase64String(screenshot5.Result.Data);
                    File.WriteAllBytes("output5.png", data5);
                    Console.WriteLine("Screenshot 5 stored");

                    //deserialise cookies
                    //set cookies
                    JArray ja = JArray.Parse(json);
                    List<Protocol.Chrome.Network.Cookie> cookiesList = new List<Protocol.Chrome.Network.Cookie>();
                    foreach (var cookie in ja)
                    {
                        Console.WriteLine(cookie["Name"]);
                        Console.WriteLine(cookie["Value"]);
                        Console.WriteLine(cookie["Domain"]);
                        Console.WriteLine(cookie["Path"]);
                        Console.WriteLine(cookie["Expires"]);
                        Console.WriteLine(cookie["Size"]);
                        Console.WriteLine(cookie["HttpOnly"]);
                        Console.WriteLine(cookie["Secure"]);
                        Console.WriteLine(cookie["Session"]);
                        Console.WriteLine(cookie["SameSite"]);

                        double expires = Double.Parse(cookie["Expires"].ToString());
                        bool secure = (cookie["Secure"].ToString() == "true") ? true : false;
                        bool httpOnly = (cookie["HttpOnly"].ToString() == "true") ? true : false;

                        var setCookie = await chromeSession.SendAsync(new Protocol.Chrome.Network.SetCookieCommand
                        {
                            Name= cookie["Name"].ToString(),
                            Value= cookie["Value"].ToString(),
                            Domain = cookie["Domain"].ToString(),
                            Path = cookie["Path"].ToString(),
                            Secure = secure,
                            HttpOnly = httpOnly,
                            SameSite = cookie["SameSite"].ToString(),
                            Expires = expires
                        });
                    }
                    await Task.Delay(1000);


                    //check cookies settings
                    var cookiesForCheck2 = (await chromeSession.SendAsync(new Protocol.Chrome.Network.GetAllCookiesCommand())).Result.Cookies;
                    var navigateResponse3 = await chromeSession.SendAsync(new NavigateCommand
                    {
                        Url = "http://mybot.su/login.php"
                    });

                    Console.WriteLine("Taking screenshot 6");
                    var screenshot6 = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });
                    var data6 = Convert.FromBase64String(screenshot6.Result.Data);
                    File.WriteAllBytes("output6.png", data6);
                    Console.WriteLine("Screenshot 6 stored");

                    Console.WriteLine("Exiting ..");
                    //await chromeSession.SendAsync(new Protocol.Chrome.Network.ClearBrowserCookiesCommand());

                    await Task.Delay(3000);
                
            }).Wait();
        }
    }
}