using System.Xml.Serialization;
using Torch;

namespace TorchMonitor
{
    public sealed class TorchMonitorFeature : ViewModel
    {
        string _name;
        bool _enabled;

        public TorchMonitorFeature()
        {
        }

        public TorchMonitorFeature(string name, bool enabled)
        {
            _name = name;
            _enabled = enabled;
        }

        [XmlAttribute]
        public string Name
        {
            get => _name;
            set => SetValue(ref _name, value);
        }

        [XmlAttribute]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        public void Deconstruct(out string name, out bool enabled)
        {
            name = _name;
            enabled = _enabled;
        }
    }
}