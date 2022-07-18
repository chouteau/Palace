namespace PalaceServer.Pages;

public partial class Hosts : ComponentBase
{
    [Inject] ILogger<Index> Logger { get; set; }
    [Inject] Services.MicroServiceCollectorManager MicroServicesCollector { get; set; }
    [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }

    public IEnumerable<Models.PalaceInfo> PalaceInfoList { get; set; }

    protected override void OnInitialized()
    {
        MicroServicesCollector.OnChanged += async () => 
        {
            await InvokeAsync(() =>
            {
                UpdateLists();
                base.StateHasChanged();
            });
        };
        UpdateLists();
    }

    void UpdateLists()
    {
        PalaceInfoList = PalaceInfoManager.GetPalaceInfoList();
    }

}
