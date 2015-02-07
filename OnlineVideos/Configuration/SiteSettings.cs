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
        [XmlEnum(Name = "WMP")] WMP,
        [XmlEnum(Name = "VLC")] VLC,
        [XmlEnum(Name = "Browser")] Browser
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
	public class SiteSettings : MarshalByRefObject
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
		public bool ShouldSerializeCategories() { return Categories != null && Categories.Count > 0; }

		protected DateTime dynamicCategoriesDiscoveryTime = DateTime.MinValue;
		protected bool dynamicCategoriesDiscovered;

        [XmlIgnore]
        public bool DynamicCategoriesDiscovered 
		{
			get
			{
				if (dynamicCategoriesDiscovered && Categories != null && Categories.Count > 0 && (DateTime.Now - dynamicCategoriesDiscoveryTime).TotalMinutes > OnlineVideoSettings.Instance.DynamicCategoryTimeout)
				{
					dynamicCategoriesDiscovered = false;
					int i = 0;
					while (i < Categories.Count)
					{
						if (!Categories[i].IsDeserialized) Categories.RemoveAt(i);
						else i++;
					}
				}
				return dynamicCategoriesDiscovered;
			}
			set { dynamicCategoriesDiscovered = value; if (value) dynamicCategoriesDiscoveryTime = DateTime.Now; }
		}

        [OnSerializing()]
        internal void OnSerializingMethod(StreamingContext context)
        {
			if (context.State == StreamingContextStates.CrossAppDomain) return;
            // remove all Categories that are dynamically discovered before serializing to xml
            if (Categories != null)
            {
                int i = 0;
                while (i < Categories.Count)
                {
                    if (!Categories[i].IsDeserialized) Categories.RemoveAt(i);
                    else i++;
                }
                DynamicCategoriesDiscovered = false; // have the siteutil re-discover them the next time
            }
        }

        public void AddCategoryForSerialization(Category cat)
        {
            cat.IsDeserialized = true;
            Categories.Add(cat);
        }
        
        public override string ToString() { return Name; }

		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion
    }

    [DataContract]
    [KnownType(typeof(RssLink))]
    [KnownType(typeof(Group))]
    [Serializable]
    [XmlInclude(typeof(RssLink))]
    [XmlInclude(typeof(Group))]
	public class Category : SearchResultItem, IComparable<Category> 
    {
        protected string _Name;

        [DataMember(Name = "name", Order = 0)]
        [XmlAttribute("name")]
        public string Name { get { return _Name; } set { if (_Name != value) { _Name = value; NotifyPropertyChanged("Name"); } } }

        [XmlIgnore]
        public virtual bool HasSubCategories { get; set; }

		protected DateTime subCategoriesDiscoveryTime = DateTime.MinValue;
		protected bool subCategoriesDiscovered;

		[XmlIgnore]
        public bool SubCategoriesDiscovered
		{
			get
			{
				if (subCategoriesDiscovered && SubCategories != null && SubCategories.Count > 0 && (DateTime.Now - subCategoriesDiscoveryTime).TotalMinutes > OnlineVideoSettings.Instance.DynamicCategoryTimeout)
				{
					subCategoriesDiscovered = false;
					int i = 0;
					while (i < SubCategories.Count)
					{
						if (!SubCategories[i].IsDeserialized) SubCategories.RemoveAt(i);
						else i++;
					}
				}
				return subCategoriesDiscovered;
			}
			set { subCategoriesDiscovered = value; if (value) subCategoriesDiscoveryTime = DateTime.Now; }
		}

        [DataMember(Name = "SubCategories", Order = 3, EmitDefaultValue = false)]
        public List<Category> SubCategories { get; set; }
		public bool ShouldSerializeSubCategories() { return SubCategories != null && SubCategories.Count > 0; }

        [OnSerializing()]
        internal void OnSerializingMethod(StreamingContext context)
        {
			if (context.State == StreamingContextStates.CrossAppDomain) return;
            // remove all SubCategories that are dynamically discovered before serializing to xml
            if (SubCategories != null)
            {
                int i = 0;
                while (i < SubCategories.Count)
                {
                    if (!SubCategories[i].IsDeserialized) SubCategories.RemoveAt(i);
                    else i++;
                }
                SubCategoriesDiscovered = false; // have the siteutil re-discover them the next time
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
			if (context.State == StreamingContextStates.CrossAppDomain) return;
            IsDeserialized = true;
            if (SubCategories != null && SubCategories.Count > 0)
            {
                foreach (Category c in SubCategories) c.ParentCategory = this;
                HasSubCategories = true;
            }
        }

        public void AddSubCategoryForSerialization(Category cat)
        {
            cat.IsDeserialized = true;
            if (SubCategories == null) SubCategories = new List<Category>();
            SubCategories.Add(cat);
        }

        protected internal bool IsDeserialized { get; set; }

        [XmlIgnore]
        public Category ParentCategory { get; set; }

        public override string ToString() { return Name; }

        public string RecursiveName(string divider = " / ")
        {
            string result = "";
            Category c = this;
            while (c != null)
            {
                result = c.Name + (result == "" ? "" : divider) + result;
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
    }

    [DataContract]
    [Serializable]
    public class RssLink : Category
    {
        protected string _Url;

        [DataMember]
        [XmlText]
        public string Url { get { return _Url; } set { if (_Url != value) { _Url = value; NotifyPropertyChanged("Url"); } } }
        
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

	/// <summary>
	/// Special class to indicate that a list of <see cref="Category"/> items has another page.
	/// </summary>
	public class NextPageCategory : RssLink
	{
		public NextPageCategory() 
		{
			Name = Translation.Instance.NextPage;
		}
	}

    [DataContract]
    [Serializable]
    public class Channel : MarshalByRefObject, INotifyPropertyChanged
    {
        protected string _StreamName;

        [DataMember(Name="name", Order=0)]
        [XmlAttribute("name")]
        public string StreamName { get { return _StreamName; } set { if (_StreamName != value) { _StreamName = value; NotifyPropertyChanged("StreamName"); } } }

        [DataMember(Name = "thumb", Order=1, EmitDefaultValue = false)]
        [XmlAttribute("thumb")]
        public string Thumb { get; set; }

        [DataMember(Order=2)]
        [XmlText]
        public string Url { get; set; }

        #region INotifyPropertyChanged Member
        [field: NonSerialized]
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region HelperClass to Serialize a Dictionary of strings

    [Serializable]
    public class StringHash: Dictionary<string, string>, IXmlSerializable
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

    #endregion

}
