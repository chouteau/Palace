namespace PalaceServer.Services
{
    public class PalaceInfoManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo> _palaceInfoDictionary;

        public PalaceInfoManager(ILogger<PalaceInfoManager> logger)
        {
            this.Logger = logger;   
            _palaceInfoDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo>();
        }

        protected ILogger Logger { get; }

        public Models.PalaceInfo GetOrCreatePalaceInfo(string userAgent, string userHostAddress)
        {
            var palaceInfo = new Models.PalaceInfo(userAgent, userHostAddress);
            var result = _palaceInfoDictionary.GetOrAdd(palaceInfo.Key, palaceInfo);
            return result;
        }

        public IEnumerable<Models.PalaceInfo> GetPalaceInfoList()
        {
            return _palaceInfoDictionary.Values;
        }



    }
}
