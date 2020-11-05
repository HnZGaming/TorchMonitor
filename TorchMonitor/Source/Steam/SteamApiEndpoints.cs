using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TorchMonitor.Steam.Models;

namespace TorchMonitor.Steam
{
    // https://developer.valvesoftware.com/wiki/Steam_Web_API
    public sealed class SteamApiEndpoints : IDisposable
    {
        sealed class SteamResponse<T>
        {
            [JsonProperty("response")]
            public T Response { get; private set; }
        }

        const string Base = "http://api.steampowered.com";
        readonly string _apiKey;
        readonly HttpClient _httpClient;

        public SteamApiEndpoints(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Steam API Key null or empty");
            }
            
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public void Dispose() => _httpClient.Dispose();

        string MakeSteamApiResourceUrl(string resourcePath, params (string Key, string Value)[] query)
        {
            var queryList = query.ToList();
            queryList.Add(("key", _apiKey));

            var queryStr = string.Join("&", queryList.Select(q => $"{q.Key}={q.Value}"));
            return $"{Base}/{resourcePath}?{queryStr}";
        }

        async Task<T> Get<T>(string resource, params (string, string)[] query)
        {
            var url = MakeSteamApiResourceUrl(resource, query);
            using (var res = await _httpClient.GetAsync(url).ConfigureAwait(false))
            {
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get '{resource}' for '{res.ReasonPhrase}' ({res.StatusCode})");
                }

                var json = await res.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeObject<SteamResponse<T>>(json);
                return content.Response;
            }
        }

        public async Task<IEnumerable<SteamPlayerSummary>> GetPlayerSummaries(params ulong[] steamIds)
        {
            const string Resource = "ISteamUser/GetPlayerSummaries/v0002";
            var steamIdQuery = string.Join(",", steamIds);
            var summaries = await Get<SteamPlayerSummaries>(Resource, ("steamids", steamIdQuery));
            return summaries.List;
        }

        public async Task<IEnumerable<SteamOwnedGame>> GetOwnedGames(ulong steamId)
        {
            const string Resource = "IPlayerService/GetOwnedGames/v0001";
            var ownedGames = await Get<SteamOwnedGames>(Resource, ("steamid", $"{steamId}"));
            return ownedGames.List;
        }
    }
}