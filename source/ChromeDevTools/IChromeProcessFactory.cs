namespace MasterDevs.ChromeDevTools
{
    public interface IChromeProcessFactory
    {
        IChromeProcess Create(int port, bool headless, string proxyServer = null, string path = null, string proxyProcol = null);
    }
}