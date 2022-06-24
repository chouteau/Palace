
namespace Palace.Services
{
    public interface IStarter
    {
        int InstanciedServiceCount { get; }
        int RunningServiceCount { get; }

        Models.MicroServiceInfo GetMicroServiceInfo(string serviceName);
        Models.MicroServiceSettings GetMicroServiceSettings(string serviceName);

        Task<bool> GetApplyAction();
        Task<bool> ApplyAction(Models.MicroServiceSettings item, PalaceServer.Models.NextActionResult action);
        Task<List<(Models.MicroServiceSettings Settings, PalaceServer.Models.NextActionResult NextAction)>> CheckHealth();
        Task CheckUpdate();
        Task CheckRemove();
        Task Start();
        Task Stop();
    }
}