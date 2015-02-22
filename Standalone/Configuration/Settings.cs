using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;

namespace Standalone.Configuration
{
    [DataContract(Name = "OnlineVideos", Namespace = "http://schemas.datacontract.org/2004/07/Standalone")]
	public class Settings
	{
		const string FileName = "OnlineVideoSettings.xml";

		public static void Load(string writeableBaseDir)
		{
			string filePath = Path.Combine(writeableBaseDir, FileName);
			if (File.Exists(filePath))
			{
				try
				{
                    using (var fs = File.OpenRead(filePath))
                    {
                        Instance = new DataContractSerializer(typeof(Settings)).ReadObject(fs) as Settings;
                    }
				}
				catch (Exception ex)
				{
					OnlineVideos.OnlineVideoSettings.Instance.Logger.Error(ex);
					Instance = new Settings();
				}
			}
			else
			{
				Instance = new Settings();
			}
			Instance.FilePath = filePath;
		}

		public void Save()
		{
            using (var writer = XmlWriter.Create(FilePath, new XmlWriterSettings() { Indent = true, CloseOutput = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true }))
            {
                new DataContractSerializer(typeof(Settings)).WriteObject(writer, this);
                writer.Flush();
                writer.Close();
            }
		}

		public static Settings Instance { get; private set; }

		public string FilePath { get; private set; }

		[DataMember]
		public DateTime LastAutoUpdate { get; set; }
	}
}
