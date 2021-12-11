
namespace Palace
{
    public interface IStarter
    {
        int InstanciedServiceCount { get; }

        Task CheckHealth();
        Task CheckUpdate();
        Task GetAction();
        Task Start();
        Task Stop();
    }
}