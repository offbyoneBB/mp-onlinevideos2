using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace OnlineVideos
{
    [Serializable]
    public enum PlayerType
    {
        [XmlEnum(Name = "Auto")] Auto,
        [XmlEnum(Name = "Internal")] Internal,
        [XmlEnum(Name = "WMP")] WMP
    }

    [DataContract(Name = "OnlineVideoSites")]
    [Serializable]
    [XmlRoot("OnlineVideoSites")]
    public class SerializableSettings
    {
        [DataMember]   
        [XmlArray("Sites"), XmlArrayItem("Site")]
        public BindingList<SiteSettings> Sites { get; set; }
    }

    [DataContract(Name="Site")]
    [Serializable]
    public class SiteSettings
    {
        public SiteSettings()
        {
            Language = "";
            Categories = new BindingList<Category>();
        }

        [DataMember(Name = "name", Order = 0)]
        [XmlAttribute("name")]
        public string Name { get; set; }

        [DataMember(Name = "util", Order = 1)]
        [XmlAttribute("util")]
        public string UtilName { get; set; }

        [DataMember(Name = "agecheck", Order = 2)]
        [XmlAttribute("agecheck")]
        public bool ConfirmAge { get; set; }

        [DataMember(Name = "enabled", Order = 3)]
        [XmlAttribute("enabled")]
        public bool IsEnabled { get; set; }

        [DataMember(Name = "lang", EmitDefaultValue = false, Order = 4)]
        [XmlAttribute("lang")]
        public string Language { get; set; }
        public bool ShouldSerializeLanguage() { return !string.IsNullOrEmpty(Language); }

        [DataMember(Name = "player", Order = 5, EmitDefaultValue = false)]
        [XmlAttribute("player")]
        public PlayerType Player { get; set; }
        public bool ShouldSerializePlayer() { return Player != PlayerType.Auto; }

        [DataMember(Name = "lastUpdated", Order = 6, EmitDefaultValue = false)]
        [XmlAttribute("lastUpdated")]
        public DateTime LastUpdated { get; set; }
        public bool ShouldSerializeLastUpdated() { return LastUpdated > new DateTime(2000,1,1); }

        [DataMember(EmitDefaultValue = false, Order = 7)]
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrEmpty(Description); }

        [DataMember(EmitDefaultValue = false, Order = 8)]
        public StringHash Configuration { get; set; }
        public bool ShouldSerializeConfiguration() { return Configuration != null && Configuration.Count > 0; }

        [DataMember(EmitDefaultValue = false, Order = 9)]
        public BindingList<Category> Categories { get; set; }
               
        [XmlIgnore]
        public bool DynamicCategoriesDiscovered { get; set; }        
        
        public override string ToString() { return Name; }
    }

    [DataContract]
    [KnownType(typeof(RssLink))]
    [KnownType(typeof(Group))]
    [Serializable]
    [XmlInclude(typeof(RssLink))]
    [XmlInclude(typeof(Group))]
    public class Category : IComparable<Category>, INotifyPropertyChanged
    {
        [DataMember(Name="name")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        [DataMember(Name="thumb", EmitDefaultValue = false)]
        [XmlAttribute("thumb")]
        public string Thumb { get; set; }

        [DataMember(Name = "desc", EmitDefaultValue = false)]
        [XmlAttribute("desc")]
        public string Description { get; set; }

        [XmlIgnore]
        public virtual bool HasSubCategories { get; set; }

        [XmlIgnore]
        public bool SubCategoriesDiscovered { get; set; }

        [XmlIgnore]
        public List<Category> SubCategories { get; set; }

        [XmlIgnore]
        public Category ParentCategory { get; set; }

        [XmlIgnore]
        public object Other { get; set; }

        public override string ToString() { return Name; }

        public string RecursiveName()
        {
            string result = "";
            Category c = this;
            while (c != null)
            {
                result = c.Name + (result == "" ? "" : " / ") + result;
                c = c.ParentCategory;
            }
            return result;
        }
        
        #region IComparable<Category> Member

        public int CompareTo(Category other)
        {
            return Name.CompareTo(other.Name);
        }

        #endregion

        #region INotifyPropertyChanged Member
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    [DataContract]
    [Serializable]
    public class RssLink : Category
    {
        [DataMember]
        [XmlText]
        public string Url { get; set; }
        
        [XmlIgnore]
        public uint? EstimatedVideoCount  { get; set; }
    }

    [DataContract]
    [Serializable]
    public class Group : Category
    {
        [DataMember]
        public BindingList<Channel> Channels { get; set; }        
    }

    [DataContract]
    [Serializable]
    public class Channel
    {
        [DataMember(Name="name", Order=0)]
        [XmlAttribute("name")]
        public string StreamName { get; set; }

        [DataMember(Name = "thumb", Order=1, EmitDefaultValue = false)]
        [XmlAttribute("thumb")]
        public string Thumb { get; set; }

        [DataMember(Order=2)]
        [XmlText]
        public string Url { get; set; }
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
