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
        string jsonServicesContent = string.Empty;
        Pages.Components.CustomValidator customValidator = new();
        Components.Toast toast;

        protected override void OnInitialized()
        { 
            PalaceInfo = PalaceInfoManager.GetPalaceInfoList().SingleOrDefault(i => i.Key == PalaceName);
            jsonServicesContent = System.Text.Json.JsonSerializer.Serialize(PalaceInfo.MicroServiceSettingsList, new System.Text.Json.JsonSerializerOptions
			{
				WriteIndented = true
			});
		}

        void ConfirmRemove(string serviceName)
        {
            ConfirmDialog.Tag = serviceName;
            ConfirmDialog.ShowDialog($"Confirm remove {serviceName} service ?");
        }

        void RemoveService(object serviceName)
        {
            PalaceInfoManager.RemoveMicroServiceSettings(PalaceInfo, $"{serviceName}");
            StateHasChanged();
        }

        protected void ValidateAndSave()
        {
            IEnumerable<Models.MicroServiceSettings> unserialized = null;
            try
			{
                unserialized = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<Models.MicroServiceSettings>>(jsonServicesContent);
			}
            catch(Exception ex)
			{
                customValidator.DisplayErrors(ex.Message);
                return;
            }

			foreach (var item in unserialized)
			{
                var errors = PalaceInfoManager.SaveMicroServiceSettings(PalaceInfo, item);

                if (errors != null
                    && errors.Any())
                {
                    customValidator.DisplayErrors(errors);
                    return;
                }
            }

            toast.Show("All services saved", ToastLevel.Success);
		}
    }
}
