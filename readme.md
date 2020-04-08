# Palace (2.5.17.0)
Generic windows services hoster, palace lauch any poco type with this caracteristics :

* Type name must terminate with "ServiceHost" suffix
 
> Ex : Sample**ServiceHost**

* Type must contains **Initialize** Method
* Type must contains **Start** Method
* Type must contains **Stop** Method

```c#
public class SampleServiceHost : IDisposable
{
	private System.Timers.Timer m_Timer;

   	public void Initialize()
	{
		System.Diagnostics.Trace.WriteLine("SampleService : Initialized");
	}

	public void Start()
	{
		m_Timer = new System.Timers.Timer();
		m_Timer.Interval = 2 * 1000;
		m_Timer.Elapsed += (s, arg) =>
		{
			System.Diagnostics.Trace.WriteLine(String.Format("SampleService : {0}", DateTime.Now));
		};
		m_Timer.Start();
	}

	public void Stop()
	{
		m_Timer.Stop();
	}

	public void Dispose()
	{
		m_Timer = null;
	}

}
```

## Installation

Using NuGet :

```sh
PM> Install-Package Palace
```


## Setup
Put json file (palace.json.config) on same directory of Palace.exe with this settings :

```json
{
	"ServiceName": "Palace",
	"ServiceDisplayName": "Palace Services Hoster",
	"ServiceDescription": "Host for services",
	"ApiKey": null
}
```

## Usage

* Reference this assembly with nuget on your Class Library Project.
* Configure build events

![Build events configuration][BuildEvents]

* Configure debug

![Build events configuration][Debug]

* Run

![Build events configuration][Run]


## Service Install / Uninstall

Lauch command line tool cmd in administrator mode in installation folder
```sh
Microsoft Windows [Version 10.0.14371]
(c) 2016 Microsoft Corporation. All rights reserved.

C:\MyService> Palace.exe /install
```

```sh
Microsoft Windows [Version 10.0.14371]
(c) 2016 Microsoft Corporation. All rights reserved.

C:\MyService> Palace.exe /uninstall
```

**Palace** run with "Network Service" account


[BuildEvents]:https://raw.githubusercontent.com/chouteau/palace/master/BuildEvents.PNG
[Debug]:https://raw.githubusercontent.com/chouteau/palace/master/Debug.PNG
[Run]:https://raw.githubusercontent.com/chouteau/palace/master/Run.png