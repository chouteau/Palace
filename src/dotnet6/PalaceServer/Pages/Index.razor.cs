using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace PalaceServer.Pages
{
    public partial class Index
    {
        [Inject] ILogger<Index> Logger { get; set; }
        [Inject] Services.MicroServiceCollectorManager MicroServicesCollector { get; set; }
        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }


        public List<Models.AvailablePackage> AvailableMicroServiceList { get; set; }
        public List<Models.ExtendedRunningMicroServiceInfo> RunningMicroServiceList { get; set; }
        public IEnumerable<Models.PalaceInfo> PalaceInfoList { get; set; }

        public void Stop(Models.ExtendedRunningMicroServiceInfo ermsi)
        {
            ermsi.NextAction = Models.ServiceAction.Stop;
        }

        public void Start(Models.ExtendedRunningMicroServiceInfo ermsi)
        {
            ermsi.NextAction = Models.ServiceAction.Start;
        }

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
            AvailableMicroServiceList = MicroServicesCollector.GetAvailableList();
            RunningMicroServiceList = MicroServicesCollector.GetRunningList();
            PalaceInfoList = PalaceInfoManager.GetPalaceInfoList();
        }

    }
}
