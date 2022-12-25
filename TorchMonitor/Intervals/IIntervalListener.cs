namespace Intervals
{
    public interface IIntervalListener
    {
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Called every interval
        /// </summary>
        /// <param name="intervalsSinceStart">Count of interval since the beginning of monitoring.</param>
        void OnInterval(int intervalsSinceStart);
    }
}