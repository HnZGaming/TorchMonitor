using Newtonsoft.Json;

namespace TorchMonitor.Ipstack
{
    // https://ipstack.com/quickstart
    public sealed class IpstackLocation
    {
        [JsonProperty("ip")]
        public string IpAddress { get; private set; }

        [JsonProperty("continent_code")]
        public string ContinentCode { get; private set; }

        [JsonProperty("continent_name")]
        public string ContinentName { get; private set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; private set; }

        [JsonProperty("country_name")]
        public string CountryName { get; private set; }

        [JsonProperty("region_code")]
        public string RegionCode { get; private set; }

        [JsonProperty("region_name")]
        public string RegionName { get; private set; }

        [JsonProperty("city")]
        public string CityName { get; private set; }
    }
}