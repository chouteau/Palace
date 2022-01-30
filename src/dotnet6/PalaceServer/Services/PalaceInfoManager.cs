﻿namespace PalaceServer.Services
{
    public class PalaceInfoManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo> _palaceInfoDictionary;

        public PalaceInfoManager(ILogger<PalaceInfoManager> logger,
            Configuration.PalaceServerSettings palaceServerSettings)
        {
            this.Logger = logger;   
            this.PalaceServerSettings = palaceServerSettings;   
            _palaceInfoDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo>();
        }

        protected ILogger Logger { get; }
        protected Configuration.PalaceServerSettings PalaceServerSettings { get; }

        public Models.PalaceInfo GetOrCreatePalaceInfo(string userAgent, string userHostAddress)
        {
            var palaceInfo = new Models.PalaceInfo(userAgent, userHostAddress, PalaceServerSettings);
            var result = _palaceInfoDictionary.GetOrAdd(palaceInfo.Key, palaceInfo);
            return result;
        }

        public IEnumerable<Models.PalaceInfo> GetPalaceInfoList()
        {
            return _palaceInfoDictionary.Values;
        }



    }
}
