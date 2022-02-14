using Microsoft.AspNetCore.Components;
using PalaceServer.Pages.Components;
using System.Text.Json.Nodes;

namespace PalaceServer.Pages
{
    public partial class MicroServiceSettingsList
    {
        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }

        ConfirmDialog ConfirmDialog { get; set; }

        [Parameter]
        public string PalaceName { get; set; }

        public Models.PalaceInfo PalaceInfo { get; set; }

        protected override void OnInitialized()
        { 
            PalaceInfo = PalaceInfoManager.GetPalaceInfoList().SingleOrDefault(i => i.Key == PalaceName);
        }

        void ConfirmRemove(string serviceName)
        {
            ConfirmDialog.Tag = serviceName;
            ConfirmDialog.ShowDialog($"Confirm remove {serviceName} service ?");
            Console.WriteLine(ConfirmDialog);
        }

        void RemoveService(object serviceName)
        {
            PalaceInfoManager.RemoveMicroServiceSettings(PalaceInfo, $"{serviceName}");
            StateHasChanged();
        }
    }
}
