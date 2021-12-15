using Microsoft.AspNetCore.Components;
using System.Text.Json.Nodes;

namespace PalaceServer.Pages
{
    public partial class ConfigurePalaceInfo
    {
        [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }

        [Parameter]
        public string PalaceName { get; set; }

        public Models.PalaceInfo PalaceInfo { get; set; }
        public string Configuration { get; set; }
        public Services.CustomValidator CustomValidator { get; set; } = new();

        protected override void OnInitialized()
        { 
            PalaceInfo = PalaceInfoManager.GetPalaceInfoList().SingleOrDefault(i => i.Key == PalaceName);
            var node = JsonNode.Parse(PalaceInfo.RawJsonConfiguration);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };
            Configuration = node.ToJsonString(options);
        }

        protected void Validate()
        {
            try
            {
                JsonNode.Parse(Configuration);
                PalaceInfo.RawJsonConfiguration = Configuration;
                PalaceInfo.LastConfigurationUpdate = DateTime.Now;
            }
            catch(Exception ex)
            {
                var errors = new Dictionary<string, List<string>>();
                errors.Add(nameof(Configuration), new List<string> { ex.Message });
                CustomValidator.DisplayErrors(errors);
            }
        }
    }
}
