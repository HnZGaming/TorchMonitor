﻿using System;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.Session;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Views;

namespace TorchUtils
{
    internal static class TorchPluginUtils
    {
        public static bool TryFindConfigFile<T>(this TorchPluginBase self, string fileName, out T foundConfig) where T : class
        {
            var filePath = Path.Combine(self.StoragePath, fileName);
            return XmlUtils.TryLoadXmlFile(filePath, out foundConfig);
        }

        public static void CreateConfigFile<T>(this TorchPluginBase self, string fileName, T content)
        {
            var filePath = Path.Combine(self.StoragePath, fileName);
            XmlUtils.SaveOrCreateXmlFile(filePath, content);
        }

        public static string MakeConfigFilePath(this TorchPluginBase self)
        {
            return Path.Combine(self.StoragePath, $"{self.GetType().Name}.cfg");
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

        public static UserControl GetOrCreateUserControl<T>(this Persistent<T> self, ref UserControl userControl) where T : class, new()
        {
            if (userControl != null) return userControl;

            userControl = new PropertyGrid
            {
                DataContext = self.Data,
            };

            return userControl;
        }
    }
}