using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TorchMonitor.Ipstack
{
    // https://ipstack.com/quickstart
    public sealed class IpstackEndpoints : IDisposable
    {
        public const string ApiKeyPlaceholder = "APIKEY";
        const string Base = "http://api.steampowered.com";
        readonly string _apiKey;
        readonly HttpClient _httpClient;

        public IpstackEndpoints(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == ApiKeyPlaceholder)
            {
                throw new Exception("Ipstack API Key null, empty or placeholder");
            }

            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public void Dispose() => _httpClient.Dispose();

        public async Task<IpstackLocation> Query(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new Exception("IP address null or empty");
            }

            var url = $"{Base}/{ipAddress}?access_key={_apiKey}";
            using (var res = await _httpClient.GetAsync(url).ConfigureAwait(false))
            {
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get '{ipAddress}' for '{res.ReasonPhrase}' ({res.StatusCode})");
                }

                var json = await res.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IpstackLocation>(json);
            }
        }
    }
}