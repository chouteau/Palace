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


        public List<Models.ExtendedRunningMicroServiceInfo> RunningMicroServiceList { get; set; }

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

    }
}
