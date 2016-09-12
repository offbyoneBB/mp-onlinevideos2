using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using System.Collections.Generic;

namespace OnlineVideos.MediaPortal2
{
    public class SiteGroupViewModel : ListItem
    {
        public SiteGroupViewModel(string groupName, string groupThumb, List<string> sitenames)
            : base(Consts.KEY_NAME, groupName)
        {
            _thumbProperty = new WProperty(typeof(string), groupThumb);
            _sitesProperty = new WProperty(typeof(List<string>), sitenames);
        }

        protected AbstractProperty _thumbProperty;
        public AbstractProperty ThumbProperty { get { return _thumbProperty; } }
        public string Thumb
        {
            get { return (string)_thumbProperty.GetValue(); }
            set { _thumbProperty.SetValue(value); }
        }

        protected AbstractProperty _sitesProperty;
        public AbstractProperty SitesProperty { get { return _sitesProperty; } }
        public List<string> Sites
        {
            get { return (List<string>)_sitesProperty.GetValue(); }
            set { _sitesProperty.SetValue(value); }
        }

    }
}
