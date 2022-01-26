
namespace Palace.Services
{
    public interface IStarter
    {
        int InstanciedServiceCount { get; }
        int RunningServiceCount { get; }

        Models.MicroServiceInfo GetMicroServiceInfo(string serviceName);
        Models.MicroServiceSettings GetMicroServiceSettings(string serviceName);

        Task<bool> ApplyAction();
        Task CheckHealth();
        Task CheckUpdate();
        Task CheckRemove();
        Task Start();
        Task Stop();
    }
}