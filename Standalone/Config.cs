using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;

namespace Standalone
{
	[DataContract(Name = "OnlineVideos")]
	public class Config
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
                        Instance = new DataContractSerializer(typeof(Config)).ReadObject(fs) as Config;
                    }
				}
				catch (Exception ex)
				{
					OnlineVideos.OnlineVideoSettings.Instance.Logger.Error(ex);
					Instance = new Config();
				}
			}
			else
			{
				Instance = new Config();
			}
			Instance.FilePath = filePath;
		}

		public void Save()
		{
			var writer = XmlWriter.Create(FilePath, new XmlWriterSettings() { Indent = true, CloseOutput = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true });
			new DataContractSerializer(typeof(Config)).WriteObject(writer, this);
			writer.Close();
		}

		public static Config Instance { get; private set; }

		public string FilePath { get; private set; }

		[DataMember]
		public DateTime LastAutoUpdate { get; set; }
	}
}
