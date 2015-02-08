using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace OnlineVideos.Hoster
{
	/// <summary>
	/// The abstract base class for all hosters. 
    /// Instances might be hosted in a seperate AppDomain than the main application, so it can be unloaded at runtime.
	/// </summary>
    public abstract class HosterBase : UserConfigurable
    {
        #region UserConfigurable implementation

        internal override string GetConfigurationKey(string fieldName)
        {
            return string.Format("{0}|{1}", Helpers.FileUtils.GetSaveFilename(GetHosterUrl()), fieldName);
        }

        #endregion

        #region User Configurable Settings

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), Description("You can give every Hoster a Priority, to control where in the list they appear (the higher the earlier). -1 will hide this Hoster, 0 is the default.")]
        protected int Priority = 0;

        public int UserPriority { get { return Priority; } }

        #endregion

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
                    SetUserConfigurationValue(field, attrs[0] as CategoryAttribute);
                }
            }
        }

        public virtual Dictionary<string, string> GetPlaybackOptions(string url)
        {
            return new Dictionary<string, string>() { { GetType().Name, GetVideoUrl(url) } };
        }
        
		public virtual Dictionary<string, string> GetPlaybackOptions(string url, System.Net.IWebProxy proxy)
		{
			return new Dictionary<string, string>() { { GetType().Name, GetVideoUrl(url) } };
		}
        
        public abstract string GetVideoUrl(string url);

        public abstract string GetHosterUrl();

        public override string ToString()
        {
            return GetHosterUrl();
        }
    }
}
