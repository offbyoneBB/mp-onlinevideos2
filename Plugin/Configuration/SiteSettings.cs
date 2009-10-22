using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using OnlineVideos.Sites;
using System.ComponentModel;

namespace OnlineVideos
{
    [Serializable]
    public enum PlayerType
    {
        [XmlEnum(Name = "Auto")] Auto,
        [XmlEnum(Name = "Internal")] Internal,
        [XmlEnum(Name = "WMP")] WMP
    }

    [Serializable]
    [XmlRoot("OnlineVideoSites")]
    public class SerializableSettings
    {                
        [XmlArray("Sites"), XmlArrayItem("Site")]
        public BindingList<SiteSettings> Sites { get; set; }
    }

    [Serializable]
    public class SiteSettings
    {
        public SiteSettings()
        {
            Language = "";
            Categories = new BindingList<Category>();
        }
        
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("util")]
        public string UtilName { get; set; }
       
        [XmlAttribute("agecheck")]
        public bool ConfirmAge { get; set; }
        
        [XmlAttribute("enabled")]
        public bool IsEnabled { get; set; }

        [XmlAttribute("player")]
        public PlayerType Player { get; set; }
        public bool ShouldSerializePlayer() { return Player != PlayerType.Auto; }

        [XmlAttribute("lang")]
        public string Language { get; set; }
        public bool ShouldSerializeLanguage() { return !string.IsNullOrEmpty(Language); }

        public string Username { get; set; }

        public string Password { get; set; }

        public string SearchUrl { get; set; }

        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrEmpty(Description); }

        public StringHash Configuration { get; set; }
        public bool ShouldSerializeConfiguration() { return Configuration != null && Configuration.Count > 0; }

        public BindingList<Category> Categories { get; set; }
               
        [XmlIgnore]
        internal bool DynamicCategoriesDiscovered { get; set; }        
        
        public override string ToString() { return Name; }
    }

    [Serializable]
    [XmlInclude(typeof(RssLink))]
    [XmlInclude(typeof(Group))]
    public class Category : IComparable<Category>
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("thumb")]
        public string Thumb { get; set; }

        [XmlAttribute("desc")]
        public string Description { get; set; }

        [XmlIgnore]
        public bool HasSubCategories { get; set; }

        [XmlIgnore]
        public bool SubCategoriesDiscovered { get; set; }

        [XmlIgnore]
        public List<Category> SubCategories { get; set; }

        [XmlIgnore]
        public Category ParentCategory { get; set; }

        public override string ToString() { return Name; }
        
        #region IComparable<Category> Member

        public int CompareTo(Category other)
        {
            return Name.CompareTo(other.Name);
        }

        #endregion
    }

    [Serializable]
    public class RssLink : Category
    {        
        [XmlText]
        public string Url { get; set; }
        
        [XmlIgnore]
        internal uint EstimatedVideoCount  { get; set; }
    }

    [Serializable]
    public class Group : Category
    {
        public BindingList<Channel> Channels { get; set; }        
    }

    [Serializable]
    public class Channel
    {        
        [XmlAttribute("name")]
        public string StreamName { get; set; }
        
        [XmlText]
        public string Url { get; set; }

        [XmlAttribute("thumb")]
        public string Thumb { get; set; }
    }

    #region HelperClass to Serialize a Dictionary of strings

    [Serializable]
    public class StringHash: Dictionary<string, string>, IXmlSerializable
    {
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
                string key = reader.GetAttribute("key");
                reader.ReadStartElement("item");
                string value = reader.ReadContentAsString();
                reader.ReadEndElement();
                this.Add(key, value);
                reader.MoveToContent();
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

    #endregion

}
