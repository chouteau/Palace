namespace PalaceServer.Models;

public class ExtendedRunningMicroServiceInfo : PalaceClient.RunningMicroserviceInfo
{
    public ExtendedRunningMicroServiceInfo()
    {
        CreationDate = DateTime.Now;
        LastUpdateDate = DateTime.Now;
        UIDisplayMore = false;
    }

    public DateTime CreationDate { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public PalaceInfo PalaceInfo { get; set; }
    public ServiceAction NextAction { get; set; }
    public bool UIDisplayMore { get; set; }

    public string Key
    {
        get
        {
            return $"{PalaceInfo.MachineName}.{PalaceInfo.HostName}.{ServiceName}".ToLower();
        }
    }

	public override string ToString()
	{
		var piList = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		var sb = new System.Text.StringBuilder();
		foreach (var item in piList)
		{
			sb.AppendLine($"{item.Name} = {item.GetValue(this)}");
		}
		return sb.ToString();
	}
}
