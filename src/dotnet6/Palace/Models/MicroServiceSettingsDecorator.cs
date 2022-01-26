using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Models
{
    internal class MicroServiceSettingsDecorator
    {
        MicroServiceSettings _inner;
        public MicroServiceSettingsDecorator(MicroServiceSettings inner)
        {
            _inner = inner;
        }

        public bool IsDirty { get; set; }

        public string ServiceName
        {
            get => _inner.ServiceName;
            set 
            {
                if (value != _inner.ServiceName)
                {
                    IsDirty = true;
                }
            }
        }
        public string MainAssembly
        {
            get => _inner.MainAssembly;
            set
            {
                if (value != _inner.MainAssembly)
                {
                    IsDirty = true;
                }
            }
        }

        public string InstallationFolder
        {
            get => _inner.InstallationFolder;
            set
            {
                if (value != _inner.InstallationFolder)
                {
                    IsDirty = true;
                }
            }
        }

        public string Arguments
        {
            get => _inner.Arguments;
            set
            {
                if (value != _inner.Arguments)
                {
                    IsDirty = true;
                }
            }
        }

        public string AdminServiceUrl
        {
            get => _inner.AdminServiceUrl;
            set
            {
                if (value != _inner.AdminServiceUrl)
                {
                    IsDirty = true;
                }
            }
        }

        public bool AlwaysStarted
        {
            get => _inner.AlwaysStarted;
            set
            {
                if (value != _inner.AlwaysStarted)
                {
                    IsDirty = true;
                }
            }
        }

        public string PalaceApiKey
        {
            get => _inner.PalaceApiKey;
            set
            {
                if (value != _inner.PalaceApiKey)
                {
                    IsDirty = true;
                }
            }
        }

        public string PackageFileName
        {
            get => _inner.PackageFileName;
            set
            {
                if (value != _inner.PackageFileName)
                {
                    IsDirty = true;
                }
            }
        }

        public string SSLCertificate
        {
            get => _inner.SSLCertificate;
            set
            {
                if (value != _inner.SSLCertificate)
                {
                    IsDirty = true;
                }
            }
        }


    }
}
