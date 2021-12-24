using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public class MicroServicesCollectionManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.MicroServiceSettings> _list;

        public MicroServicesCollectionManager(Configuration.PalaceSettings palaceSettings,
            ILogger<MicroServicesCollectionManager> logger)
        {
            this.PalaceSettings = palaceSettings;
            this.Logger = logger;
            _list = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.MicroServiceSettings>();
            try
            {
                BindFromFileName();
            }
            catch (Exception ex)
            {
                Logger.LogCritical("Bind micro services collection from fileName fail with error : {0}", ex.Message);
            }
        }

        protected Configuration.PalaceSettings PalaceSettings { get; set; }
        protected ILogger Logger { get; set; }

        internal void BindFromFileName()
        {
            if (System.IO.File.Exists(PalaceSettings.PalaceServicesFileName))
            {
                var content = System.IO.File.ReadAllText(PalaceSettings.PalaceServicesFileName);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<Models.MicroServiceSettings>>(content);
                foreach (var item in list)
                {
                    Add(item);                    
                }
            }
        }

        public void Add(Models.MicroServiceSettings microServiceSettings)
        {
            if (microServiceSettings == null)
            {
                Logger.LogWarning("microServiceSettings is null");
                return;
            }
            if (_list.Keys.Any(i => i.Equals(microServiceSettings.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Logger.LogWarning("microServiceSettings {0} already referenced", microServiceSettings.ServiceName);
                return;
            }

            var validate = Validate(microServiceSettings);
            if (!validate.IsValid)
            {
                Logger.LogWarning("microServiceSettings {0} is invalid\r{1}", microServiceSettings.ServiceName, string.Join("\r", validate.BrokenRules));
                return;
            }

            _list.TryAdd(microServiceSettings.ServiceName, microServiceSettings);
        }

        public IEnumerable<Models.MicroServiceSettings> GetList()
        { 
            return _list.Values;
        }

        internal (bool IsValid, List<string> BrokenRules) Validate(Models.MicroServiceSettings mss)
        {
            var result = true;
            var brokenRules = new List<string>();
            if (string.IsNullOrWhiteSpace(mss.ServiceName))
            {
                brokenRules.Add("Service name is null or empty");
                result = false;
            }
            else if (_list.Any(i => i.Key.Equals(mss.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
            {
                brokenRules.Add("Service name already exists");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(mss.MainAssembly))
            {
                brokenRules.Add("MainAssembly is null or empty");
                result = false;
            }
            if (string.IsNullOrWhiteSpace(mss.PackageFileName))
            {
                brokenRules.Add("PackageFileName is null or empty");
                result = false;
            }
            else if (!mss.PackageFileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                brokenRules.Add("PackageFileName is not zip file");
                result = false;
            }
            if (string.IsNullOrWhiteSpace(mss.AdminServiceUrl))
            {
                brokenRules.Add("AdminServiceUrl is null or empty");
                result = false;
            }
            else
            {
                try
                {
                    new Uri(mss.AdminServiceUrl);
                }
                catch
                {
                    brokenRules.Add("AdminServiceUrl is not uri");
                    result = false;
                }
            }


            return (result, brokenRules);
        }
    }
}
