using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using TorchUtils;

namespace Ipstack
{
    // https://ipstack.com/quickstart
    public sealed class IpstackEndpoints : IDisposable
    {
        const string Base = "http://api.ipstack.com";
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IIpstackConfig _config;
        readonly HttpClient _httpClient;

        public IpstackEndpoints(IIpstackConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        public void Dispose() => _httpClient.Dispose();

        public async Task<IpstackLocation> GetLocationOrNullAsync(string ipAddress)
        {
            ipAddress.ThrowIfNullOrEmpty(nameof(ipAddress));

            var apiKey = _config.ApiKey;
            if (string.IsNullOrEmpty(apiKey)) return null; // not enabled

            var url = $"{Base}/{ipAddress}?access_key={apiKey}";
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