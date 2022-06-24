using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using PalaceServer.Pages.Components;
using System.Security.Claims;

namespace PalaceServer.Pages
{
    public partial class Packages
    {
        [Inject] ILogger<Packages> Logger { get; set; }
        [Inject] Services.MicroServiceCollectorManager MicroServicesCollector { get; set; }
        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }
        [Inject] Configuration.PalaceServerSettings PalaceServerSettings { get; set; }

        ConfirmDialog ConfirmDialog { get; set; }
        string errorReport { get; set; }

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
            AvailablePackageList = MicroServicesCollector.GetAvailablePackageList();
        }

        void ConfirmRemove(string packageName)
        {
            ConfirmDialog.Tag = packageName;
            ConfirmDialog.ShowDialog($"Confirm remove {packageName} package ?");
        }

        async Task RemovePackage(object packageFileName)
        {
            var result = await MicroServicesCollector.RemovePackage($"{packageFileName}");
            if (result != null)
            {
                errorReport = result;
            }
            StateHasChanged();
        }
    }
}
