using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace OnlineVideos.Helpers
{
    /// <summary>
    /// helper class to serialize a dictionary of strings
    /// </summary>
    [Serializable]
    public class StringHash : Dictionary<string, string>, IXmlSerializable
    {
        public StringHash() : base() { }

        protected StringHash(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty) return;
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                if (reader.IsEmptyElement)
                {
                    reader.Read();
                }
                else
                {
                    string key = reader.GetAttribute("key");
                    reader.ReadStartElement("item");
                    string value = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    this.Add(key, value);
                    reader.MoveToContent();
                }
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (string key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeString("key", key);
                writer.WriteCData(this[key]);
                writer.WriteEndElement();
            }
        }
    }
}
