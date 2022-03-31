using System.Windows;

namespace TorchMonitor
{
    public partial class TorchMonitorControl
    {
        readonly TorchMonitorPlugin _plugin;

        public TorchMonitorControl(TorchMonitorPlugin plugin)
        {
            _plugin = plugin;
            DataContext = TorchMonitorConfig.Instance;
            InitializeComponent();
        }
    }
}