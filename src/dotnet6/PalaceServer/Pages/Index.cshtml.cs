using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PalaceServer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Services.MicroServiceCollectorManager _microServicesService;
        private readonly Services.PalaceInfoManager _palaceInfoManager;

        public IndexModel(ILogger<IndexModel> logger,
            Services.MicroServiceCollectorManager microServicesService,
            Services.PalaceInfoManager palaceInfoManager)
        {
            _logger = logger;
            _microServicesService = microServicesService;
            _palaceInfoManager = palaceInfoManager;
        }

        public List<Models.AvailableMicroServiceInfo> AvailableMicroServiceList { get; set; }
        public List<Models.ExtendedRunningMicroServiceInfo> RunningMicroServiceList { get; set; }
        public IEnumerable<Models.PalaceInfo> PalaceInfoList { get; set; }

        public void OnGet()
        {
            AvailableMicroServiceList = _microServicesService.GetAvailableList();
            RunningMicroServiceList = _microServicesService.GetRunningList();
            PalaceInfoList = _palaceInfoManager.GetPalaceInfoList();
        }
    }
}