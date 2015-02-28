using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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

        public void Serialize(Stream stream)
        {
            var ctx = new StreamingContext();
            foreach (var site in Sites)
            {
                site.OnSerializingMethod(ctx);
                CallOnSerializingRecursive(site.Categories, ctx);
            }
            var ser = new XmlSerializer(typeof(SerializableSettings));
            ser.Serialize(XmlWriter.Create(stream, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true }), this);
        }

        public static IList<SiteSettings> Deserialize(string siteXml)
        {
            siteXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OnlineVideoSites xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Sites>
" + siteXml + @"
</Sites>
</OnlineVideoSites>";
            return CrossDomain.OnlineVideosAppDomain.PluginLoader.CreateSiteSettingsFromXml(siteXml);
        }

        public static IList<SiteSettings> Deserialize(TextReader reader)
        {
            var ser = new XmlSerializer(typeof(SerializableSettings));
            SerializableSettings s = ser.Deserialize(reader) as SerializableSettings;
            if (s != null)
            {
                var ctx = new System.Runtime.Serialization.StreamingContext();
                foreach (var site in s.Sites) CallOnDeserializedRecursive(site.Categories, ctx);
                return s.Sites;
            }
            else
            {
                return null;
            }
        }

        private static void CallOnDeserializedRecursive(IList<Category> cats, System.Runtime.Serialization.StreamingContext ctx)
        {
            if (cats != null)
            {
                foreach (var cat in cats)
                {
                    cat.OnDeserializedMethod(ctx);
                    CallOnDeserializedRecursive(cat.SubCategories, ctx);
                }
            }
        }

        private static void CallOnSerializingRecursive(IList<Category> cats, System.Runtime.Serialization.StreamingContext ctx)
        {
            if (cats != null)
            {
                foreach (var cat in cats)
                {
                    cat.OnSerializingMethod(ctx);
                    CallOnSerializingRecursive(cat.SubCategories, ctx);
                }
            }
        }
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
        public Helpers.StringHash Configuration { get; set; }
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

        /// <summary>
        /// Find and set all configuration fields that do not have their default value
        /// </summary>
        /// <param name="siteUtil"></param>
        public void AddConfigurationValues(Sites.SiteUtilBase siteUtil)
        {
            // 1. build a list of all the Fields that are used for OnlineVideosConfiguration
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            foreach (FieldInfo field in siteUtil.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false);
                if (attrs.Length > 0 && ((System.ComponentModel.CategoryAttribute)attrs[0]).Category == "OnlineVideosConfiguration")
                {
                    fieldInfos.Add(field);
                }
            }

            // 2. get a "clean" site by creating it with empty SiteSettings
            Configuration = new Helpers.StringHash();
            Sites.SiteUtilBase cleanSiteUtil = Sites.SiteUtilFactory.CreateFromShortName(UtilName, this);

            // 3. compare and collect different settings
            foreach (FieldInfo field in fieldInfos)
            {
                object defaultValue = field.GetValue(cleanSiteUtil);
                object newValue = field.GetValue(siteUtil);
                if (defaultValue != newValue)
                {
                    // seems that if default value = false, and newvalue = false defaultvalue != newvalue returns true
                    // so added extra check
                    if (defaultValue == null || !defaultValue.Equals(newValue))
                        Configuration.Add(field.Name, newValue.ToString());
                }
            }
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
}
