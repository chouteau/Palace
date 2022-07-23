
namespace PalaceServer.Pages;

public partial class Index : ComponentBase
{
    [Inject] 
    ILogger<Index> Logger { get; set; }
    [Inject] 
    Services.MicroServiceCollectorManager MicroServicesCollector { get; set; }
    [Inject] 
    Services.PalaceInfoManager PalaceInfoManager { get; set; }

    public List<Models.ExtendedRunningMicroServiceInfo> RunningMicroServiceList { get; set; }
    string groupBy = "host";

    public void Stop(Models.ExtendedRunningMicroServiceInfo ermsi)
    {
        Logger.LogInformation("Try to stop {0} on {1}", ermsi.ServiceName, ermsi.Key);
        ermsi.NextAction = Models.ServiceAction.Stop;
    }

    public void Start(Models.ExtendedRunningMicroServiceInfo ermsi)
    {
        Logger.LogInformation("Try to start {0} on {1}", ermsi.ServiceName, ermsi.Key);
        ermsi.NextAction = Models.ServiceAction.Start;
    }

		protected override void OnAfterRender(bool firstRender)
		{
			if (firstRender)
			{
            MicroServicesCollector.OnChanged += async () =>
            {
                await InvokeAsync(() =>
                {
                    UpdateLists();
                    base.StateHasChanged();
                });
            };
        }
    }

	protected override void OnInitialized()
    {
        UpdateLists();
    }

    void UpdateLists()
    {
        RunningMicroServiceList = MicroServicesCollector.GetRunningList();
    }

    void DisplayMore(Models.ExtendedRunningMicroServiceInfo info)
		{
        info.UIDisplayMore = !info.UIDisplayMore;
        StateHasChanged();
		}

    string GetHostNameByIp(string ip)
		{
        var hostList = PalaceInfoManager.GetPalaceInfoList();
        var host = hostList.FirstOrDefault(i => i.Ip == ip);
        return host.HostName;
    }

    string GetColor(Models.PalaceInfo host, Models.ExtendedRunningMicroServiceInfo msinfo)
    {
        return null;
    }

}
