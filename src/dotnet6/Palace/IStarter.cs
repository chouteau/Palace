
namespace Palace
{
    public interface IStarter
    {
        int InstanciedServiceCount { get; }

        Task<bool> ApplyAction();
        Task CheckHealth();
        Task CheckUpdate();
        Task Start();
        Task Stop();
    }
}