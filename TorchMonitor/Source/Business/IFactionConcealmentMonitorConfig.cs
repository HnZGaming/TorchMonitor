namespace TorchMonitor.Business
{
    public interface IFactionConcealmentMonitorConfig
    {
        int CollectIntervalSecs { get; }
        int WriteIntervalSecs { get; }
        string FactionTag { get; }
    }
}