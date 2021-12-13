using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PalaceServer.Pages
{
    public partial class Logs
    {
        [Inject]
        protected Services.LogCollector LogCollector { get; set; }

        public IEnumerable<Models.LogInfo> LogInfoList { get; set; }

        protected override void OnInitialized()
        {
            LogCollector.OnChanged += async () =>
            {
                UpdateLists();
                await InvokeAsync(() =>
                {
                    base.StateHasChanged();
                });
            };
            UpdateLists();
        }

        void UpdateLists()
        {
            LogInfoList = LogCollector.GetLogInfoList();
        }
    }
}
