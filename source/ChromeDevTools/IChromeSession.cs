using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MasterDevs.ChromeDevTools
{
    public interface ICommand<T>
    {

    }
    public interface IChromeSession
    {
        Task<CommandResponse<TResponse>> SendAsync<TResponse>(ICommand<TResponse> parameter, CancellationToken cancellationToken);

        Task<ICommandResponse> SendAsync<T>(CancellationToken cancellationToken);
        string ProxyUser { get; set; }
        string ProxyPass { get; set; }

        void Subscribe<T>(Action<T> handler) where T : class;
        void ProxyAuthenticate(string proxyUser, string proxyPass);
        Process Process { get; set; }
    }
}