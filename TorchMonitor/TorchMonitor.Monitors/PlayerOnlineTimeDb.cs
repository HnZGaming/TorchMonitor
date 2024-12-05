using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;

namespace TorchMonitor.Monitors
{
    public sealed class PlayerOnlineTimeDb
    {
        class Entry
        {
            [JsonProperty("steam_id")]
            public ulong SteamId { get; set; }

            [JsonProperty("player_name")]
            public string PlayerName { get; set; }

            [JsonProperty("online_time")]
            public double OnlineTime { get; set; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly string _filePath;
        readonly Dictionary<ulong, Entry> _entries;

        internal PlayerOnlineTimeDb(string filePath)
        {
            _filePath = filePath;
            _entries = new Dictionary<ulong, Entry>();
        }

        public void Read()
        {
            if (!File.Exists(_filePath)) return;

            try
            {
                var text = File.ReadAllText(_filePath);
                var entries = JsonConvert.DeserializeObject<Entry[]>(text);
                foreach (var entry in entries)
                {
                    _entries[entry.SteamId] = entry;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void IncrementPlayerOnlineTime(ulong steamId, string playerName, double addedOnlineTime)
        {
            if (_entries.TryGetValue(steamId, out var entry))
            {
                entry.PlayerName = playerName;
                entry.OnlineTime += addedOnlineTime;
            }
            else // new entry
            {
                _entries[steamId] = new Entry
                {
                    SteamId = steamId,
                    PlayerName = playerName,
                    OnlineTime = addedOnlineTime,
                };
            }
        }

        public double GetPlayerOnlineTime(ulong steamId)
        {
            return _entries.GetValueOrDefault(steamId)?.OnlineTime ?? 0d;
        }

        public double GetTotalOnlineTime()
        {
            return _entries.Values.Sum(d => d.OnlineTime);
        }

        public void Write()
        {
            var entries = _entries.Values.OrderBy(v => v.SteamId).ToArray();
            var text = JsonConvert.SerializeObject(entries);
            File.WriteAllText(_filePath, text);
        }
    }
}