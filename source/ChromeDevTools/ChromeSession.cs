#if !NETSTANDARD1_5
using MasterDevs.ChromeDevTools.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;
using Newtonsoft.Json.Linq;

namespace MasterDevs.ChromeDevTools
{
    public class ChromeSession : IChromeSession
    {
        private readonly string _endpoint;
        private readonly ConcurrentDictionary<string, ConcurrentBag<Action<object>>> _handlers = new ConcurrentDictionary<string, ConcurrentBag<Action<object>>>();
        private ICommandFactory _commandFactory;
        private IEventFactory _eventFactory;
        private ManualResetEvent _openEvent = new ManualResetEvent(false);
        private ManualResetEvent _publishEvent = new ManualResetEvent(false);
        private ConcurrentDictionary<long, ManualResetEventSlim> _requestWaitHandles = new ConcurrentDictionary<long, ManualResetEventSlim>();
        private ICommandResponseFactory _responseFactory;
        private ConcurrentDictionary<long, ICommandResponse> _responses = new ConcurrentDictionary<long, ICommandResponse>();
        private ConcurrentDictionary<long, ICommandResponse> _bad_responses = new ConcurrentDictionary<long, ICommandResponse>();
        private WebSocket _webSocket;
        private static object _Lock = new object();
        public Exception socketExeption;

        public string ProxyUser { get; set; }
        public string ProxyPass { get; set; }
        public Process Process { get; set; }
        public string MainSessionId { get; set; }

        public ChromeSession(string endpoint, ICommandFactory commandFactory, ICommandResponseFactory responseFactory, IEventFactory eventFactory)
        {
            _endpoint = endpoint;
            _commandFactory = commandFactory;
            _responseFactory = responseFactory;
            _eventFactory = eventFactory;
        }

        public void Dispose()
        {
            if (null == _webSocket) return;
            if (_webSocket.State == WebSocketState.Open)
            {
                _webSocket.Close();
            }
            _webSocket.Dispose();
        }

        private void EnsureInit()
        {
            if (null == _webSocket)
            {
                lock (_Lock)
                {
                    if (null == _webSocket)
                    {
                        Init().Wait();
                    }
                }
            }
        }

        private Task Init()
        {
            _openEvent.Reset();

            _webSocket = new WebSocket(_endpoint);
            _webSocket.EnableAutoSendPing = false;
            _webSocket.Opened += WebSocket_Opened;
            _webSocket.MessageReceived += WebSocket_MessageReceived;
            _webSocket.Error += WebSocket_Error;
            _webSocket.Closed += WebSocket_Closed;
            _webSocket.DataReceived += WebSocket_DataReceived;

            _webSocket.Open();
            return Task.Run(() =>
            {
                _openEvent.WaitOne();
            });
        }

        public Task<ICommandResponse> SendAsync<T>(CancellationToken cancellationToken)
        {
            var command = _commandFactory.Create<T>();
            return SendCommand(command, cancellationToken);
        }

        public Task<CommandResponse<T>> SendAsync<T>(ICommand<T> parameter, CancellationToken cancellationToken)
        {
            var command = _commandFactory.Create(parameter);
            var task = SendCommand(command, cancellationToken);
            return CastTaskResult<ICommandResponse, CommandResponse<T>>(task);
        }

        private Task<TDerived> CastTaskResult<TBase, TDerived>(Task<TBase> task) where TDerived : TBase
        {
            var tcs = new TaskCompletionSource<TDerived>();
            task.ContinueWith(t => tcs.SetResult((TDerived)t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(t => tcs.SetException(t.Exception.InnerExceptions), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => tcs.SetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
            return tcs.Task;
        }

        /*
        private Task<TDerived> CastTaskResult<TBase, TDerived>(Task<TBase> task) where TDerived: TBase
        {
            var tcs = new TaskCompletionSource<TDerived>();
            task.ContinueWith(t => tcs.SetResult((TDerived)t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(t => tcs.SetException(t.Exception.InnerExceptions), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => tcs.SetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
            return tcs.Task;
        }
        */
        /*
        private Task<TDerived> CastTaskResult<TBase, TDerived>(Task<TBase> task) where TDerived : TBase
        {
            var tcs = new TaskCompletionSource<TDerived>();
            task.ContinueWith(
                delegate (Task<TBase> t)
                {
                    try
                    {
                        TDerived res = (TDerived)t.Result;
                        tcs.SetResult(res);
                    }
                    catch (System.Exception ex)
                    {
                        ErrorResponse error = t.Result as ErrorResponse;

                        if (error != null)
                        {
                            string errorMessage = "";

                            try
                            {
                                errorMessage += "Id " + System.Convert.ToString(error.Id, System.Globalization.CultureInfo.InvariantCulture) + System.Environment.NewLine;
                            }
                            catch
                            { }


                            try
                            {
                                errorMessage += "Error " + System.Convert.ToString(error.Error.Code, System.Globalization.CultureInfo.InvariantCulture) + System.Environment.NewLine;
                            }
                            catch
                            { }


                            try
                            {
                                errorMessage += error.Error.Message + System.Environment.NewLine;
                            }
                            catch
                            { }


                            try
                            {
                                errorMessage += error.Method;
                            }
                            catch
                            { }

                            var exx = new Exception() { Source = "1" };
                            tcs.SetException(new Exception(errorMessage, ex));
                        }
                        else
                            tcs.SetException(ex);
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(
                delegate (Task<TBase> t)
                {
                    try
                    {
                        tcs.SetException(t.Exception.InnerExceptions);
                    }
                    catch (System.Exception ex)
                    {
                        tcs.SetException(ex);
                    }


                }, TaskContinuationOptions.OnlyOnFaulted
                );
            task.ContinueWith(
                delegate (Task<TBase> t)
                {
                    try
                    {
                        tcs.SetCanceled();
                    }
                    catch (System.Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, TaskContinuationOptions.OnlyOnCanceled);
            return tcs.Task;
        }
        */

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var handlerType = typeof(T);
            var handlerForBag = new Action<object>(obj => handler((T)obj));
            _handlers.AddOrUpdate(handlerType.FullName,
                (m) => new ConcurrentBag<Action<object>>(new[] { handlerForBag }),
                (m, currentBag) =>
                {
                    currentBag.Add(handlerForBag);
                    return currentBag;
                });
        }

        private void HandleEvent(IEvent evnt)
        {
            if (null == evnt
                || null == evnt)
            {
                return;
            }
            var type = evnt.GetType().GetGenericArguments().FirstOrDefault();
            if (null == type)
            {
                return;
            }
            var handlerKey = type.FullName;
            ConcurrentBag<Action<object>> handlers = null;
            if (_handlers.TryGetValue(handlerKey, out handlers))
            {
                var localHandlers = handlers.ToArray();
                foreach (var handler in localHandlers)
                {
                    ExecuteHandler(handler, evnt);
                }
            }
        }

        private void ExecuteHandler(Action<object> handler, dynamic evnt)
        {
            if (evnt.GetType().GetGenericTypeDefinition() == typeof(Event<>))
            {
                handler(evnt.Params);
            }
            else
            {
                handler(evnt);
            }
        }

        private void HandleResponse(ICommandResponse response)
        {
            if (null == response) return;
            ManualResetEventSlim requestMre;
            if (_requestWaitHandles.TryGetValue(response.Id, out requestMre))
            {
                _responses.AddOrUpdate(response.Id, id => response, (key, value) => response);
                requestMre.Set();
            }
            else
            {
                // in the case of an error, we don't always get the request Id back :(
                // if there is only one pending requests, we know what to do ... otherwise
                if (1 == _requestWaitHandles.Count)
                {
                    var requestId = _requestWaitHandles.Keys.First();
                    _requestWaitHandles.TryGetValue(requestId, out requestMre);
                    _responses.AddOrUpdate(requestId, id => response, (key, value) => response);
                    requestMre.Set();
                }
            }
        }

        private Task<ICommandResponse> SendCommand(Command command, CancellationToken cancellationToken)
        {
            if (MainSessionId != null) command.SessionId = MainSessionId;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new MessageContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };
            var requestString = JsonConvert.SerializeObject(command, settings);
            Console.WriteLine("SEND >>> " + requestString);
            var requestResetEvent = new ManualResetEventSlim(false);
            _requestWaitHandles.AddOrUpdate(command.Id, requestResetEvent, (id, r) => requestResetEvent);
            return Task.Run(() =>
            {
                EnsureInit();
                _webSocket.Send(requestString);
                requestResetEvent.Wait(cancellationToken);
                ICommandResponse response = null;
                _responses.TryRemove(command.Id, out response);
                _requestWaitHandles.TryRemove(command.Id, out requestResetEvent);
                Console.WriteLine("RECIVE <<< Id:" + response.Id + " method:" + ((response.Method == null) ? "null" : response.Method));
                return response;
            });
        }

        private bool TryGetCommandResponse(byte[] data, out ICommandResponse response)
        {
            response = _responseFactory.Create(data);
            return null != response;
        }

        private bool TryGetCommandResponse(string message, out ICommandResponse response)
        {
            response = _responseFactory.Create(message);
            return null != response;
        }

        private bool TryGetEvent(byte[] data, out IEvent evnt)
        {
            evnt = _eventFactory.Create(data);
            return null != evnt;
        }

        private bool TryGetEvent(string message, out IEvent evnt)
        {
            evnt = _eventFactory.Create(message);
            return null != evnt;
        }

        private void WebSocket_Closed(object sender, EventArgs e)
        {
        }

        private void WebSocket_DataReceived(object sender, WebSocket4Net.DataReceivedEventArgs e)
        {
            ICommandResponse response;
            if (TryGetCommandResponse(e.Data, out response))
            {
                HandleResponse(response);
                return;
            }
            IEvent evnt;
            if (TryGetEvent(e.Data, out evnt))
            {
                HandleEvent(evnt);
                return;
            }
            throw new Exception("Don't know what to do with response: " + e.Data);
        }



        private void WebSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            socketExeption = e.Exception;
            HandleError(e.Exception);
        }

        private void HandleError(Exception ex)
        {
            while(_requestWaitHandles.Count>0)
            {
                var reqId = _requestWaitHandles.Keys.First(); ;
                var requestWaitHandle = _requestWaitHandles.Where(r => r.Key == reqId).First().Value;

                ManualResetEventSlim mrs = new ManualResetEventSlim();

                JObject jo = new JObject(new JProperty("error", 
                    new JObject(
                        new JProperty("Message", ex.Message), new JProperty("Code", "1"))));

                ICommandResponse response = _responseFactory.Create(jo.ToString()) ;

                _responses.AddOrUpdate(reqId, id => response, (key, value) => response);
                requestWaitHandle.Set();

                _requestWaitHandles.TryRemove(reqId,out mrs);
            }            
        }

        private void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ICommandResponse response;
            if (TryGetCommandResponse(e.Message, out response))
            {
                HandleResponse(response);
                return;
            }
            IEvent evnt;
            if (TryGetEvent(e.Message, out evnt))
            {
                HandleEvent(evnt);
                return;
            }
            //throw new Exception("Don't know what to do with response: " + e.Message);
        }

        private void WebSocket_Opened(object sender, EventArgs e)
        {
            _openEvent.Set();
        }

        async public void ProxyAuthenticate(string proxyUser, string proxyPass)
        {
            this.ProxyUser = proxyUser;
            this.ProxyPass = proxyPass;
        }
    }
}
#endif
