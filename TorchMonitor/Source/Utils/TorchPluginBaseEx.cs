using System.IO;
using System.Xml.Serialization;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;

namespace TorchMonitor.Utils
{
    public abstract class TorchPluginBaseEx : TorchPluginBase
    {
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += (session, state) =>
            {
                OnGameStateChanged(state);
            };
        }

        void OnGameStateChanged(TorchSessionState newState)
        {
            if (newState == TorchSessionState.Loaded)
            {
                OnGameLoaded();
            }

            if (newState == TorchSessionState.Unloading)
            {
                OnGameUnloading();
            }
        }

        virtual protected void OnGameLoaded()
        {
        }

        virtual protected void OnGameUnloading()
        {
        }

        protected bool TryFindConfigFile<T>(string fileName, out T foundConfig) where T : class
        {
            var filePath = Path.Combine(StoragePath, fileName);
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

        protected void CreateConfigFile<T>(string fileName, T content)
        {
            var filePath = Path.Combine(StoragePath, fileName);
            using (var file = File.CreateText(filePath))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(file, content);
            }
        }
    }
}