using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Core.General;

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

        protected AbstractProperty _hasFocus;
        public AbstractProperty HasFocusProperty { get { return _hasFocus; } }
        public bool HasFocus
        {
            get { return (bool)_hasFocus.GetValue(); }
            set { _hasFocus.SetValue(value); }
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
            _hasFocus = new WProperty(typeof(bool), false);
        }
    }
}
