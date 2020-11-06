using System;
using System.IO;
using System.Xml.Serialization;
using Torch;
using Torch.Session;
using Torch.API.Managers;
using Torch.API.Session;

namespace TorchUtils
{
    internal static class TorchPluginUtils
    {
        public static bool TryFindConfigFile<T>(this TorchPluginBase self, string fileName, out T foundConfig) where T : class
        {
            var filePath = Path.Combine(self.StoragePath, fileName);
            if (!File.Exists(filePath))
            {
                foundConfig = default;
                return false;
            }

            using (var file = File.OpenText(filePath))
            {
                var serializer = new XmlSerializer(typeof(T));
                foundConfig = serializer.Deserialize(file) as T;
                return foundConfig != null;
            }
        }

        public static void CreateConfigFile<T>(this TorchPluginBase self, string fileName, T content)
        {
            var filePath = Path.Combine(self.StoragePath, fileName);
            using (var file = File.CreateText(filePath))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(file, content);
            }
        }

        public static void ListenOnGameLoaded(this TorchPluginBase self, Action f)
        {
            var sessionManager = self.Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += (session, state) =>
            {
                if (state == TorchSessionState.Loaded)
                {
                    f();
                }
            };
        }

        public static void ListenOnGameUnloading(this TorchPluginBase self, Action f)
        {
            var sessionManager = self.Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += (session, state) =>
            {
                if (state == TorchSessionState.Unloading)
                {
                    f();
                }
            };
        }
    }
}