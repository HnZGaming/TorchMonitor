using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Game.Multiplayer;
using Utils.General;

namespace Utils.Torch
{
    public sealed class SimDropObserver
    {
        readonly double[] _recentSims;
        int _intervalCount;

        public SimDropObserver(int bufferCount)
        {
            _recentSims = Enumerable.Repeat(1d, bufferCount).ToArray();
        }

        public async Task Main(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var sim = Sync.ServerSimulationRatio;
                _recentSims[_intervalCount++ % _recentSims.Length] = sim;

                await Task.Delay(1.Seconds(), cancellationToken);
            }
        }

        public bool IsLaggierThan(double sim)
        {
            return _recentSims.Average() < sim;
        }
    }
}