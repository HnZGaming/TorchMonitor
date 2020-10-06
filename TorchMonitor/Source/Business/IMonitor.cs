namespace TorchMonitor.Business
{
    public interface IMonitor
    {
        /// <summary>
        /// Called every interval
        /// </summary>
        /// <param name="intervalsSinceStart">Count of interval since the beginning of monitoring.</param>
        void OnInterval(int intervalsSinceStart);
    }
}