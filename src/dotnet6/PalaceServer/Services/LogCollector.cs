namespace PalaceServer.Services
{
    public class LogCollector
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.LogInfo> _logDic;

        public event Action OnChanged;

        public LogCollector(Configuration.PalaceServerSettings palaceServerSettings)
        {
            this._logDic = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.LogInfo>();
            this.PalaceServerSettings = palaceServerSettings;
        }

        protected Configuration.PalaceServerSettings PalaceServerSettings { get; }

        public void AddLog(Models.LogInfo logInfo)
        {
            if (_logDic.Count > PalaceServerSettings.LogCountMax)
            {
                var first = _logDic.First();
                _logDic.Remove(first.Key, out var byebye);
            }
            _logDic.TryAdd(logInfo.LogId, logInfo);
            OnChanged?.Invoke();
        }

        public void Clear()
        {
            _logDic.Clear();
        }

        public IEnumerable<Models.LogInfo> GetLogInfoList(Func<Models.LogInfo, bool> predicate = null)
        {
            var result = _logDic.Values.OrderByDescending(i => i.CreationDate).ToList();
            if (predicate != null)
            {
                result = _logDic.Values.Where(predicate)
                            .Take(100).ToList();
            }
            return result;
        }
    }
}
