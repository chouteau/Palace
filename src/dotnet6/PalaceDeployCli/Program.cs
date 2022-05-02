
// 1 - Get command line parameters
//      --mode service --servicename xxx (update host palace)
//      --mode webserver --workerprocess xxx (update web server)

// 2 - Download latest version of service or webserver
//

// 3 -- if service
//      stop service and waiting
//      update files
//      start service

// 3bis -- if webserver
//         stop workerprocess and wait
//         update files
//         start workerprocess

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PalaceDeployCli;
using Spectre.Console;

var services = new ServiceCollection();
services.AddTransient<DownloadManager>();
services.AddTransient<IISManager>();
services.AddTransient<ServiceManager>();

var sp = services.BuildServiceProvider();

var option = Parser.Default.ParseArguments<DefaultOptions, StatusOptions>(args);
await option.MapResult(async (DefaultOptions prompt) =>
{
	AnsiConsole.WriteLine("Welcome to the Palace Deploy CLI");
	AnsiConsole.WriteLine("Choose action :");
	AnsiConsole.WriteLine();
	AnsiConsole.WriteLine("1 - Install latest version of palace host");
	AnsiConsole.WriteLine("2 - Install latest version of palace server");
	AnsiConsole.WriteLine("3 - Quit");

	var selectedAction = AnsiConsole.Prompt(
        new TextPrompt<int>("Choose :")
        );

	switch (selectedAction)
	{
		case 1:
			break;
	}
	
},
errors =>
{
    return Task.FromResult(-1);
});

return await Task.FromResult(0);

