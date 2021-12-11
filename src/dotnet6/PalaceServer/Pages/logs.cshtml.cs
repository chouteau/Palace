using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PalaceServer.Pages
{
    public class logsModel : PageModel
    {
        public logsModel(Services.LogCollector logCollector)
        {
            this.LogCollector = logCollector;
        }

        protected Services.LogCollector LogCollector { get; }
        
        public IEnumerable<Models.LogInfo> LogInfoList { get; set; }  

        public void OnGet()
        {
            LogInfoList = LogCollector.GetLogInfoList();
        }
    }
}
