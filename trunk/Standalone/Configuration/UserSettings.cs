using OnlineVideos;
using OnlineVideos.Helpers;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Standalone.Configuration
{
    public class UserSettings : MarshalByRefObject, IUserStore
    {
        const string FileName = "OnlineVideoUserSettings.xml";

        bool _hasChanges;
        string filePath;
        SerializableDictionary<string, string> _settings { get; set; }

        public UserSettings(string dir)
		{
            filePath = Path.Combine(dir, FileName);
            Load(dir);
			if (_settings == null)
				_settings = new SerializableDictionary<string, string>();
		}

        public string GetValue(string key, bool decrypt = false)
		{
			string result = null;
			_settings.TryGetValue(key, out result);
            return (result != null && decrypt) ? EncryptionUtils.SymDecryptLocalPC(result) : result;
		}

        public void SetValue(string key, string value, bool encrypt = false)
		{
			_hasChanges = true;
            if (encrypt) value = EncryptionUtils.SymEncryptLocalPC(value);
			_settings[key] = value;
		}

        void Load(string dir)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using (var fs = File.OpenRead(filePath))
                    {
                        _settings = new XmlSerializer(typeof(SerializableDictionary<string, string>)).Deserialize(fs) as SerializableDictionary<string, string>;
                    }
                }
                catch (Exception ex)
                {
                    OnlineVideos.OnlineVideoSettings.Instance.Logger.Error(ex);
                }
            }
        }

        public void SaveAll()
        {
            if (_hasChanges)
            {
                using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings() { Indent = true, CloseOutput = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true }))
                {
                    new XmlSerializer(typeof(SerializableDictionary<string, string>)).Serialize(writer, _settings);
                    writer.Flush();
                    writer.Close();
                }
                _hasChanges = false;
            }
        }

        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion
    }
}
