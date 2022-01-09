using System;

namespace PalaceClient
{
    public class RunningMicroserviceInfo
    {
        public string ServiceName { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
        public bool UserInteractive { get; set; }
        public DateTime LastWriteTime { get; set; }
        public int ThreadCount { get; set; }
        public int ProcessId { get; set; }
        public string ServiceState { get; set; }
        public DateTime StartedDate { get; set; }
        public string CommandLine { get; set; }
        public long PeakWorkingSet { get; set; }
        public long PeakPagedMem { get; set; }
        public long PeakVirtualMem { get; set; }
        public string EnvironmentName { get; set; }
        public string AdminUrl { get; set; }
        public string PalaceClientVersion { get; set; }
    }
}
