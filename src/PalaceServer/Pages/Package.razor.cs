namespace PalaceServer.Pages;

public partial class Package
{
	[Parameter] public string PackageFileName { get; set; }

	[Inject] ILogger<Package> Logger { get; set; }
	[Inject] Services.MicroServiceCollectorManager MicroServicesCollector { get; set; }
	[Inject] NavigationManager NavigationManager { get; set; }

	ConfirmDialog ConfirmDialog { get; set; }
	Models.AvailablePackage package;
	List<FileInfo> backupFileInfoList;
	string errorReport { get; set; }


	protected override void OnInitialized()
	{
		var packageList = MicroServicesCollector.GetAvailablePackageList();
		package = packageList.FirstOrDefault(i => i.PackageFileName.Equals(PackageFileName, StringComparison.InvariantCultureIgnoreCase));
		backupFileInfoList = MicroServicesCollector.GetBackupFileList(PackageFileName);
		base.OnInitialized();
	}

	void ConfirmRollback(FileInfo fileInfo)
	{
		ConfirmDialog.Tag = fileInfo;
		ConfirmDialog.ShowDialog($"Confirm rollaback {fileInfo.Name} package ?");
	}

	void RollbackPackage(object fileInfo)
	{
		var result = MicroServicesCollector.RollbackPackage(package, fileInfo as FileInfo);
		if (result != null)
		{
			errorReport = result;
		}
		NavigationManager.NavigateTo("/packages", true);
	}

}
