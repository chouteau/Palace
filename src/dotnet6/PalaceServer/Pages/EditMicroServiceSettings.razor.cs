using Microsoft.AspNetCore.Components;
using System.Text.Json.Nodes;

namespace PalaceServer.Pages
{
    public partial class EditMicroServiceSettings
    {
        bool _isNew = false;

        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        [Parameter]
        public string PalaceName { get; set; }
        [Parameter]
        public string ServiceName { get; set; }

        public Models.MicroServiceSettings MicroServiceSettings { get; set; }
        public Services.CustomValidator CustomValidator { get; set; } = new();
        public Models.PalaceInfo PalaceInfo { get; set; }

        protected override void OnInitialized()
        {
            PalaceInfo = PalaceInfoManager.GetPalaceInfoList().SingleOrDefault(i => i.Key == PalaceName);
            if (ServiceName == "new")
            {
                MicroServiceSettings = new Models.MicroServiceSettings();
                _isNew = true;
            }
            else
            {
                var existing = PalaceInfo.MicroServiceSettingsList.Single(i => i.ServiceName == ServiceName);
                MicroServiceSettings = (Models.MicroServiceSettings)existing.Clone();
            }
        }

        protected void ValidateAndSave()
        {
            Dictionary<string, List<string>> errors = null;
            if (_isNew)
            {
                errors = PalaceInfo.AddMicroServiceSettings(MicroServiceSettings);
            }
            else
            {
                errors = PalaceInfo.UpdateMicroServiceSettings(MicroServiceSettings);
            }

            if (errors != null
                && errors.Any())
            {
                CustomValidator.DisplayErrors(errors);
                return;
            }

            _isNew = false;
            NavigationManager.NavigateTo($"/palace/{PalaceInfo.Key}/services");
        }
    }
}
