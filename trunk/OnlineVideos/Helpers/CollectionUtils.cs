using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace OnlineVideos.Helpers
{
    public static class CollectionUtils
    {
        public static string DictionaryToString(Dictionary<string, string> dic)
        {
            var sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, OmitXmlDeclaration = true };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("dictionary");
                foreach (string key in dic.Keys)
                {
                    writer.WriteStartElement("item");
                    writer.WriteStartElement("key");
                    writer.WriteCData(key);
                    writer.WriteEndElement();
                    writer.WriteStartElement("value");
                    writer.WriteCData(dic[key]);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.Flush();
                writer.Close();
            }
            return sb.ToString();
        }

        public static Dictionary<string, string> DictionaryFromString(string input)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            using (XmlReader reader = XmlReader.Create(new StringReader(input)))
            {
                bool wasEmpty = reader.IsEmptyElement;
                reader.Read();
                if (wasEmpty) return null;
                reader.ReadStartElement("dictionary");
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("item");
                    reader.ReadStartElement("key");
                    string key = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    reader.ReadStartElement("value");
                    string value = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    dic.Add(key, value);
                    reader.ReadEndElement();
                    reader.MoveToContent();
                }
                reader.ReadEndElement();
            }
            return dic;
        }

        public static void Randomize<T>(this List<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
