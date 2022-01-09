using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace PalaceServer.Pages
{
    public partial class Packages
    {
        [Inject] ILogger<Index> Logger { get; set; }
        [Inject] Services.MicroServiceCollectorManager MicroServicesCollector { get; set; }
        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }


        public List<Models.AvailablePackage> AvailablePackageList { get; set; }

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
            AvailablePackageList = MicroServicesCollector.GetAvailableList();
        }

    }
}
