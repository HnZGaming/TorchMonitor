using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.General
{
    public sealed class HttpUrlBuilder
    {
        static readonly char[] _pathSeparator = {'/'};

        readonly string _hostUrl;
        readonly List<string> _pathSegments;
        readonly Dictionary<string, string> _arguments;

        public HttpUrlBuilder(string hostUrl)
        {
            hostUrl.ThrowIfNullOrEmpty(nameof(hostUrl));
            ThrowIfIllFormattedUrl(hostUrl);

            _hostUrl = hostUrl.TrimEnd(_pathSeparator);
            _pathSegments = new List<string>();
            _arguments = new Dictionary<string, string>();
        }

        static void ThrowIfIllFormattedUrl(string url)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new UriFormatException(url);
            }
        }

        public void SetPath(string path)
        {
            _pathSegments.Clear();
            var pathSegments = path.Split(_pathSeparator, StringSplitOptions.RemoveEmptyEntries);
            _pathSegments.AddRange(pathSegments);
        }

        public void AddArgument(string key, string value)
        {
            key.ThrowIfNullOrEmpty(nameof(key));
            value.ThrowIfNullOrEmpty(nameof(value));

            _arguments[Uri.EscapeUriString(key)] = Uri.EscapeUriString(value);
        }

        public Uri ToUri()
        {
            var path = _pathSegments.Any()
                ? "/" + string.Join("/", _pathSegments)
                : "";

            var query = _arguments.Any()
                ? "?" + string.Join("&", _arguments.Select(q => $"{q.Key}={q.Value}"))
                : "";

            var urlString = $"{_hostUrl}{path}{query}";
            return new Uri(urlString);
        }

        public override string ToString()
        {
            return ToUri().ToString();
        }
    }
}