using System.IO;
using System.Xml.Serialization;

namespace Utils.Torch
{
    internal static class XmlUtils
    {
        public static bool TryLoadXmlFile<T>(string filePath, out T foundConfig) where T : class
        {
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

        public static void SaveOrCreateXmlFile<T>(string filePath, T content)
        {
            using (var file = File.CreateText(filePath))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(file, content);
            }
        }
    }
}