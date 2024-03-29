﻿
namespace PalaceServer.Pages;

public partial class UploadPackage
{
    [Inject] Configuration.PalaceServerSettings PalaceServerSettings { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] ILogger<UploadPackage> Logger { get; set; }

    public Pages.Components.CustomValidator CustomValidator { get; set; } = new();
    public UploadedFile UploadedFile { get; set; } = new();

    bool uploading = false;

    public async Task LoadFile(InputFileChangeEventArgs e)
    {
        uploading = true;
        StateHasChanged();

        if (!e.File.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
        {
            CustomValidator.DisplayErrors(new Dictionary<string, List<string>>
            {
                { "FileName", new List<string> { "Zipfile only allowed" } }
            });
            return;
        }

        if (e.File.Size == 0)
        {
            CustomValidator.DisplayErrors(new Dictionary<string, List<string>>
            {
                { "FileName", new List<string> { "Zipfile is empty" } }
            });
            return;
        }
        var fileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PalaceServer", $"{Guid.NewGuid()}-{e.File.Name}");

        using (var stream = e.File.OpenReadStream(100 * 1024 * 1024))
        {
            using (var fileContent = new StreamContent(stream))
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    await fileContent.CopyToAsync(fs);
                    Logger.LogInformation("File {0} uploaded", fileName);
                }
            }
        }

        // TODO : Verifier si le zip est correct

        var finalFileName = System.IO.Path.Combine(PalaceServerSettings.MicroServiceStagingFolder, e.File.Name);
        await Task.Delay(1000);
        try
        {
            System.IO.File.Copy(fileName, finalFileName, true);
            System.IO.File.Delete(fileName);
        }
        catch (Exception ex)
        {
            CustomValidator.DisplayErrors(new Dictionary<string, List<string>>
            {
                { "FileName", new List<string> { ex.Message } }
            });
            uploading = false;
            StateHasChanged();
            return;
        }

        NavigationManager.NavigateTo("/Packages");
    }
}
