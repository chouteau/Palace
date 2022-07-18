namespace PalaceServer.Pages;

public partial class MicroServiceSettingsList
{
    [Inject] Services.PalaceInfoManager PalaceInfoManager { get; set; }
    [Inject] Services.ClipboardService ClipboardService { get; set; }


    ConfirmDialog ConfirmDialog { get; set; }

    [Parameter]
    public string PalaceName { get; set; }

    public Models.PalaceInfo PalaceInfo { get; set; }
    string jsonServicesContent = string.Empty;
    Pages.Components.CustomValidator customValidator = new();
    Components.Toast toast;
    List<Bag<MicroServiceSettings>> bagList = new();

    protected override void OnInitialized()
    { 
        PalaceInfo = PalaceInfoManager.GetPalaceInfoList().SingleOrDefault(i => i.Key == PalaceName);
		foreach (var item in PalaceInfo.MicroServiceSettingsList)
		{
            var ico = new Bag<MicroServiceSettings> { Item = item, Content = "oi oi-clipboard" };
            bagList.Add(ico);
        }

        jsonServicesContent = System.Text.Json.JsonSerializer.Serialize(PalaceInfo.MicroServiceSettingsList, new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true
		});
	}

    void ConfirmRemove(string serviceName)
    {
        ConfirmDialog.Tag = serviceName;
        ConfirmDialog.ShowDialog($"Confirm remove {serviceName} service ?");
    }

    void RemoveService(object serviceName)
    {
        PalaceInfoManager.RemoveMicroServiceSettings(PalaceInfo, $"{serviceName}");
        StateHasChanged();
    }

    protected void ValidateAndSave()
    {
        IEnumerable<Models.MicroServiceSettings> unserialized = null;
        try
		{
            unserialized = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<Models.MicroServiceSettings>>(jsonServicesContent);
		}
        catch(Exception ex)
		{
            customValidator.DisplayErrors(ex.Message);
            return;
        }

		foreach (var item in unserialized)
		{
            var errors = PalaceInfoManager.SaveMicroServiceSettings(PalaceInfo, item);

            if (errors != null
                && errors.Any())
            {
                customValidator.DisplayErrors(errors);
                return;
            }
        }

        toast.Show("All services saved", ToastLevel.Success);
	}

    async Task CopyToClipboard(object item)
	{
		var content = System.Text.Json.JsonSerializer.Serialize(item, new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true
		});
        var bag = bagList.FirstOrDefault(i => i.Item == item);
        bag.Content = "oi oi-check";
        await ClipboardService.WriteTextAsync(content);
        await Task.Delay(2 * 1000);
        StateHasChanged();		
	}
}
