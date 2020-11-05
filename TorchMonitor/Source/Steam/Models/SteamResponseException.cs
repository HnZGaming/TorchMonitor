using System;
using System.Net;

namespace TorchMonitor.Steam.Models
{
    public class SteamResponseException : Exception
    {
        public HttpStatusCode Code { get; }

        public SteamResponseException(HttpStatusCode code, string message) : base(message)
        {
            Code = code;
        }
    }
}