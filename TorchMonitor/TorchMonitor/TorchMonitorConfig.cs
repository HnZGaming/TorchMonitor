using System.Xml.Serialization;
using Ipstack;
using Torch;
using Torch.Views;
using TorchMonitor.Monitors;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig :
        ViewModel,
        IpstackEndpoints.IConfig,
        GeoLocationMonitor.IConfig
    {
        string _ipstackApiKey = "apikey";

        [XmlElement("Ipstack.ApiKey")]
        [Display(Name = "Ipstack.ApiKey")]
        public string ApiKey
        {
            get => _ipstackApiKey;
            set => SetProperty(ref _ipstackApiKey, value);
        }

        // ReSharper disable once RedundantAssignment
        void SetProperty<T>(ref T property, T value)
        {
            property = value;
            OnPropertyChanged();
        }

        bool GeoLocationMonitor.IConfig.Enabled => !string.IsNullOrEmpty(ApiKey);
    }
}