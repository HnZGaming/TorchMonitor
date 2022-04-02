using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Utils.General;

namespace TorchMonitor.Monitors
{
    public sealed class PlayerOnlineTimeDb
    {
        class Document
        {
            [JsonConstructor]
            Document()
            {
            }

            public Document(ulong steamId, double onlineTime)
            {
                SteamId = $"{steamId}";
                OnlineTime = onlineTime;
            }

            [JsonProperty("steam_id"), StupidDbId]
            public string SteamId { get; private set; }

            [JsonProperty("online_time")]
            public double OnlineTime { get; private set; }
        }

        readonly StupidDb<Document> _localDb;
        readonly Dictionary<ulong, double> _dbCopy;

        internal PlayerOnlineTimeDb(string filePath)
        {
            _localDb = new StupidDb<Document>(filePath);
            _dbCopy = new Dictionary<ulong, double>();
        }

        public void Read()
        {
            _dbCopy.Clear();
            _localDb.Read();

            foreach (var doc in _localDb.QueryAll())
            {
                if (!ulong.TryParse(doc.SteamId, out var steamId)) continue;
                _dbCopy[steamId] = doc.OnlineTime;
            }

            WriteToDb();
        }

        public void IncrementPlayerOnlineTime(ulong steamId, double addedOnlineTime)
        {
            _dbCopy.TryGetValue(steamId, out var onlineTime);
            onlineTime += addedOnlineTime;
            _dbCopy[steamId] = onlineTime;
        }

        public double GetPlayerOnlineTime(ulong steamId)
        {
            _dbCopy.TryGetValue(steamId, out var onlineTime);
            return onlineTime;
        }

        public double GetTotalOnlineTime()
        {
            return _dbCopy.Sum(p => p.Value);
        }

        public void WriteToDb()
        {
            var docs = new List<Document>();
            foreach (var (steamId, onlineTime) in _dbCopy)
            {
                var doc = new Document(steamId, onlineTime);
                docs.Add(doc);
            }

            _localDb.Clear();
            _localDb.InsertAll(docs);
            _localDb.Write();
        }
    }
}