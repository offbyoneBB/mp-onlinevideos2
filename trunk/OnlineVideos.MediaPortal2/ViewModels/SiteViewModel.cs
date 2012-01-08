using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ScreenManagement;

namespace OnlineVideos.MediaPortal2
{
    public class SiteViewModel
    {
        protected AbstractProperty _settings;
        public AbstractProperty SettingsProperty { get { return _settings; } }
        public SiteSettings Settings
        {
            get { return (SiteSettings)_settings.GetValue(); }
        }

        protected AbstractProperty _focusPrio;
        public AbstractProperty FocusPrioProperty { get { return _focusPrio; } }
		public SetFocusPriority FocusPrio
        {
			get { return (SetFocusPriority)_focusPrio.GetValue(); }
            set { _focusPrio.SetValue(value); }
        }

        protected Sites.SiteUtilBase _site;
        public Sites.SiteUtilBase Site
        {
            get { return _site; }
        }
        
        public SiteViewModel(Sites.SiteUtilBase site)
        {
            _site = site;

            _settings = new WProperty(typeof(SiteSettings), site.Settings);
			_focusPrio = new WProperty(typeof(SetFocusPriority), SetFocusPriority.None);
        }
    }
}
