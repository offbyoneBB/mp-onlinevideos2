using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OnlineVideos.Sites;
using System.ComponentModel;
using System.Reflection;
using System.Web;

namespace OnlineVideos.Hoster.Base
{
    public enum VideoType
    {
        flv,
        divx,
        unknown
    }
    public abstract class HosterBase : ICustomTypeDescriptor
    {
        protected VideoType videoType;

        public virtual Dictionary<string, string> getPlaybackOptions(string url)
        {
            return new Dictionary<string, string>() { { GetType().Name, getVideoUrls(url) } };
        }
		/*public virtual Dictionary<string, string> getPlaybackOptions(string url, System.Net.IWebProxy proxy)
		{
			return new Dictionary<string, string>() { { GetType().Name, getVideoUrls(url) } };
		}*/
        public abstract string getVideoUrls(string url);
        public abstract string getHosterUrl();
        public virtual VideoType getVideoType() { return videoType; }

        public static RegexOptions defaultRegexOptions = RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace;

        protected static string FlashProvider(string url, string webData = null)
        {
            string page = webData;
            if (webData == null)
                page = SiteUtilBase.GetWebData(url);

            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                n = Regex.Match(page, @"flashvars.file=""(?<url>[^""]+)"";");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                n = Regex.Match(page, @"flashvars.{0,50}file\s*?(?:=|:)\s*?(?:\'|"")?(?<url>[^\'""]+)(?:\'|"")?", defaultRegexOptions);
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
        protected static string DivxProvider(string url, string webData = null)
        {
            string page = webData;
            if (webData == null)
                page = SiteUtilBase.GetWebData(url);

            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"var\surl\s=\s'(?<url>[^']+)';");
                if (n.Success) return n.Groups["url"].Value;
                n = Regex.Match(page, @"video/divx""\ssrc=""(?<url>[^""]+)""");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
        }

        private static string GetVal(string num, string[] pars)
        {
            int n = 0;
            for (int i = 0; i < num.Length; i++)
            {
                n = n * 36;
                char c = num[i];
                if (Char.IsDigit(c))
                    n += ((int)c) - 0x30;
                else
                    n += ((int)c) - 0x61 + 10;
            }
            if (n < 0 || n >= pars.Length)
                return n.ToString();

            return pars[n];
        }

        public static string UnPack(string packed)
        {
            string res;
            int p = packed.IndexOf('|');
            if (p < 0) return null;
            p = packed.LastIndexOf('\'', p);

            string pattern = packed.Substring(0, p - 1);

            string[] pars = packed.Substring(p).TrimStart('\'').Split('|');
            for (int i = 0; i < pars.Length; i++)
                if (String.IsNullOrEmpty(pars[i]))
                    if (i < 10)
                        pars[i] = i.ToString();
                    else
                        if (i < 36)
                            pars[i] = ((char)(i + 0x61 - 10)).ToString();
                        else
                            pars[i] = (i - 26).ToString();
            res = String.Empty;
            string num = "";
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (Char.IsDigit(c) || Char.IsLower(c))
                    num += c;
                else
                {
                    if (num.Length > 0)
                    {
                        res += GetVal(num, pars);
                        num = "";
                    }
                    res += c;
                }
            }
            if (num.Length > 0)
                res += GetVal(num, pars);

            return res;
        }

        protected static string GetSubString(string s, string start, string until)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        protected string getRegExData(string regex, string data, string group)
        {
            string result = string.Empty;
            Match m = Regex.Match(data, regex);
            if (m.Success)
                result = m.Groups[group].Value;
            return result == null ? string.Empty : result;
        }

        public override string ToString()
        {
            return getHosterUrl();
        }

        /// <summary>
        /// You should always call this implementation, even when overriding it. It is called after the instance has been created
        /// in order to configure settings from the xml for this hoster.
        /// </summary>
        public virtual void Initialize()
        {
            // apply custom settings
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (attrs.Length > 0)
                {
                    if (((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration"
                             && OnlineVideoSettings.Instance.UserStore != null)
                    {
                        string hosterUrl = getHosterUrl();
                        string value = OnlineVideoSettings.Instance.UserStore.GetValue(string.Format("{0}|{1}", Utils.GetSaveFilename(hosterUrl), field.Name));
                        if (value != null)
                        {
                            try
                            {
                                if (field.FieldType.IsEnum)
                                {
                                    field.SetValue(this, Enum.Parse(field.FieldType, value));
                                }
                                else
                                {
                                    field.SetValue(this, Convert.ChangeType(value, field.FieldType));
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warn("{0} - ould not set User Configuration Value: {1}. Error: {2}", hosterUrl, field.Name, ex.Message);
                            }
                        }
                    }
                }
            }
        }

        [Category("OnlineVideosUserConfiguration"), Description("You can give every Hoster a Priority, to control where in the list they appear (the higher the earlier). -1 will hide this Hoster, 0 is the default.")]
        protected int Priority = 0;
        public int UserPriority { get { return Priority; } }

        #region ICustomTypeDescriptor Members

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(null);
        }

        #endregion

        #region ICustomTypeDescriptor Implementation with Fields as Properties

        private PropertyDescriptorCollection _propCache;
        private FilterCache _filterCache;

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(
            Attribute[] attributes)
        {
            bool filtering = (attributes != null && attributes.Length > 0);
            PropertyDescriptorCollection props = _propCache;
            FilterCache cache = _filterCache;

            // Use a cached version if possible
            if (filtering && cache != null && cache.IsValid(attributes))
                return cache.FilteredProperties;
            else if (!filtering && props != null)
                return props;

            // Create the property collection and filter
            props = new PropertyDescriptorCollection(null);
            foreach (PropertyDescriptor prop in
                TypeDescriptor.GetProperties(
                this, attributes, true))
            {
                props.Add(prop);
            }
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                FieldPropertyDescriptor fieldDesc =
                    new FieldPropertyDescriptor(field);
                if (!filtering ||
                    fieldDesc.Attributes.Contains(attributes))
                    props.Add(fieldDesc);
            }

            // Store the computed properties
            if (filtering)
            {
                cache = new FilterCache();
                cache.Attributes = attributes;
                cache.FilteredProperties = props;
                _filterCache = cache;
            }
            else _propCache = props;

            return props;
        }

        private class FieldPropertyDescriptor : PropertyDescriptor
        {
            private FieldInfo _field;

            public FieldPropertyDescriptor(FieldInfo field)
                : base(field.Name,
                    (Attribute[])field.GetCustomAttributes(typeof(Attribute), true))
            {
                _field = field;
            }

            public FieldInfo Field { get { return _field; } }

            public override bool Equals(object obj)
            {
                FieldPropertyDescriptor other = obj as FieldPropertyDescriptor;
                return other != null && other._field.Equals(_field);
            }

            public override int GetHashCode() { return _field.GetHashCode(); }

            public override string DisplayName
            {
                get
                {
                    var attr = _field.GetCustomAttributes(typeof(LocalizableDisplayNameAttribute), false);
                    if (attr.Length > 0)
                        return ((LocalizableDisplayNameAttribute)attr[0]).LocalizedDisplayName;
                    else
                        return base.DisplayName;
                }
            }

            public override bool IsReadOnly { get { return false; } }

            public override void ResetValue(object component) { }

            public override bool CanResetValue(object component) { return false; }

            public override bool ShouldSerializeValue(object component)
            {
                return true;
            }

            public override Type ComponentType
            {
                get { return _field.DeclaringType; }
            }

            public override Type PropertyType { get { return _field.FieldType; } }

            public override object GetValue(object component)
            {
                return _field.GetValue(component);
            }

            public override void SetValue(object component, object value)
            {
                // only set if changed
                if (_field.GetValue(component) != value)
                {
                    _field.SetValue(component, value);
                    OnValueChanged(component, EventArgs.Empty);

                    // if this field is a user config, set value also in MediaPortal config file
                    object[] attrs = _field.GetCustomAttributes(typeof(CategoryAttribute), false);
                    if (attrs.Length > 0 && ((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration")
                    {
                        string hosterUrl = (component as HosterBase).getHosterUrl();
                        OnlineVideoSettings.Instance.UserStore.SetValue(string.Format("{0}|{1}", Utils.GetSaveFilename(hosterUrl), _field.Name), value.ToString());
                    }
                }
            }
        }

        private class FilterCache
        {
            public Attribute[] Attributes;
            public PropertyDescriptorCollection FilteredProperties;
            public bool IsValid(Attribute[] other)
            {
                if (other == null || Attributes == null) return false;

                if (Attributes.Length != other.Length) return false;

                for (int i = 0; i < other.Length; i++)
                {
                    if (!Attributes[i].Match(other[i])) return false;
                }

                return true;
            }
        }

        #endregion

    }
}
