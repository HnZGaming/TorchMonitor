using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ipstack;
using NLog;
using TorchMonitor.Reflections;
using VRage.GameServices;

namespace TorchMonitor.Utils
{
    public sealed class GeoLocationCollection
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IpstackEndpoints _ipstackEndpoints;
        readonly ConcurrentDictionary<ulong, IpstackLocation> _ipLocations;

        public GeoLocationCollection(IpstackEndpoints ipstackEndpoints)
        {
            _ipstackEndpoints = ipstackEndpoints;
            _ipLocations = new ConcurrentDictionary<ulong, IpstackLocation>();
        }

        public async Task Load(ulong steamId)
        {
            // Get the public IP of the Steam player
            var state = new MyP2PSessionState();
            var networking = MySteamGameService_Networking.Value;
            networking.Peer2Peer.GetSessionState(steamId, ref state);
            var ip = BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray();
            var ipAddress = new IPAddress(ip).ToString();

            // Get the location
            var location = await _ipstackEndpoints.GetLocationOrNullAsync(ipAddress);
            _ipLocations[steamId] = location ?? new IpstackLocation();
        }

        public bool TryGetLocation(ulong steamId, out IpstackLocation location)
        {
            return _ipLocations.TryGetValue(steamId, out location);
        }
    }
}