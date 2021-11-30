namespace PalaceServer.Models
{
    public class ServiceProperties
    {
        public ServiceProperties()
        {
            PropertyList = new List<ServiceProperty>();
        }

        public void Add(string propertyName, string propertyValue)
        {
            if (PropertyList.Any(i => i.PropetyName == propertyName))
            {
                return;
            }

            PropertyList.Add(new ServiceProperty
            { 
                PropetyName = propertyName,
                PropertyValue = propertyValue
            });
        }
        public string ServiceName { get; set; }
        public IList<ServiceProperty> PropertyList { get; set; }

        public static ServiceProperties CreateChangeState(string serviceName, string state)
        {
            var result = new ServiceProperties();
            result.ServiceName = serviceName;
            result.Add("ServiceState", state);
            return result;
        }
    }
}
