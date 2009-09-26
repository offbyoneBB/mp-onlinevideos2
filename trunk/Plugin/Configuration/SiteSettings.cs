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

        string utilName;
        [XmlAttribute("util")]
        public string UtilName
        {
            get { return utilName; }
            set { utilName = value; util = SiteUtilFactory.GetByName(value); }
        }

        SiteUtilBase util;
        [XmlIgnore]
        public SiteUtilBase Util { get { return util; } }
        
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
}
