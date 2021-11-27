namespace Palace
{
	partial class ProjectInstaller
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.PalaceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.PalaceServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// PalaceProcessInstaller
			// 
			this.PalaceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.NetworkService;
			this.PalaceProcessInstaller.Password = null;
			this.PalaceProcessInstaller.Username = null;
			// 
			// PalaceServiceInstaller
			// 
			this.PalaceServiceInstaller.DelayedAutoStart = true;
			this.PalaceServiceInstaller.Description = "Palace Services Hoster";
			this.PalaceServiceInstaller.DisplayName = "Palace";
			this.PalaceServiceInstaller.ServiceName = "Palace";
			this.PalaceServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.PalaceProcessInstaller,
            this.PalaceServiceInstaller});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller PalaceProcessInstaller;
		private System.ServiceProcess.ServiceInstaller PalaceServiceInstaller;
	}
}