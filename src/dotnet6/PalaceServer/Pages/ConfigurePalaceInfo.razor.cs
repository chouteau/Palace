using Microsoft.AspNetCore.Components;

namespace PalaceServer.Pages
{
    public partial class ConfigurePalaceInfo
    {
        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }

        [Parameter]
        public string PalaceName { get; set; }

        public Models.PalaceInfo PalaceInfo { get; set; }
        public string Configuration { get; set; }

        protected override void OnInitialized()
        { 
            PalaceInfo = PalaceInfoManager.GetPalaceInfoList().SingleOrDefault(i => i.Key == PalaceName);
            Configuration = PalaceInfo.RawJsonConfiguration;
        }

        protected void Validate()
        {
            try
            {
                System.Text.Json.JsonSerializer.Deserialize(Configuration, typeof(object));
                PalaceInfo.RawJsonConfiguration = Configuration;
            }
            catch
            {
            }
        }
    }
}
