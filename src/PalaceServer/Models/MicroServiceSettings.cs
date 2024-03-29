﻿namespace PalaceServer.Models;

public class MicroServiceSettings : ICloneable
{
    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string ServiceName { get; set; }
    [Required]
    public string MainAssembly { get; set; }
    public string InstallationFolder { get; set; }
    public string Arguments { get; set; }
    [Required]
    public string AdminServiceUrl { get; set; }
    public bool AlwaysStarted { get; set; } = true;
    public string PalaceApiKey { get; set; }
    [Required]
    public string PackageFileName { get; set; }
    public string SSLCertificate { get; set; }
    public int InstanceCount { get; set; } = 1;

    public int? ThreadLimitBeforeAlert { get; set; }
    public int? ThreadLimitBeforeRestart { get; set; }

    public int? NotRespondingCountBeforeAlert { get; set; }
    public int? NotRespondingCountBeforeRestart { get; set; }

    public int? MaxWorkingSetLimitBeforeAlert { get; set; }
    public int? MaxWorkingSetLimitBeforeRestart { get; set; }

    public object Clone()
    {
        var result = (MicroServiceSettings)this.MemberwiseClone();
        result.Id = Guid.NewGuid();
        return result;
    }
}
