namespace PalaceServer.Services
{
    public class PalaceInfoManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo> _palaceInfoDictionary;

        public PalaceInfoManager()
        {
            _palaceInfoDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo>();
        }

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
