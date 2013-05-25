using System;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
    public class VideoViewModel : ListItem
    {
		protected PropertyChangedDelegator eventDelegator = null;

        protected AbstractProperty _titleProperty;
        public AbstractProperty TitleProperty { get { return _titleProperty; } }
        public string Title
        {
            get { return (string)_titleProperty.GetValue(); }
            set { _titleProperty.SetValue(value); }
        }

        protected AbstractProperty _title2Property;
        public AbstractProperty Title2Property { get { return _title2Property; } }
        public string Title2
        {
            get { return (string)_title2Property.GetValue(); }
            set { _title2Property.SetValue(value); }
        }

        protected AbstractProperty _descriptionProperty;
        public AbstractProperty DescriptionProperty { get { return _descriptionProperty; } }
        public string Description
        {
            get { return (string)_descriptionProperty.GetValue(); }
            set { _descriptionProperty.SetValue(value); }
        }

        protected AbstractProperty _lengthProperty;
        public AbstractProperty LengthProperty { get { return _lengthProperty; } }
        public string Length
        {
            get { return (string)_lengthProperty.GetValue(); }
            set { _lengthProperty.SetValue(value); }
        }

		protected AbstractProperty _airdateProperty;
		public AbstractProperty AirdateProperty { get { return _airdateProperty; } }
		public string Airdate
		{
			get { return (string)_airdateProperty.GetValue(); }
			set { _airdateProperty.SetValue(value); }
		}

        protected AbstractProperty _thumbnailImageProperty;
        public AbstractProperty ThumbnailImageProperty { get { return _thumbnailImageProperty; } }
        public string ThumbnailImage
        {
            get { return (string)_thumbnailImageProperty.GetValue(); }
            set { _thumbnailImageProperty.SetValue(value); }
        }

        public VideoInfo VideoInfo { get; protected set; }

		public string SiteName { get; protected set; }
		public string SiteUtilName { get; protected set; }

        public VideoViewModel(string title, string thumbImage)
			: base(Consts.KEY_NAME, title)
        {
            _titleProperty = new WProperty(typeof(string), title);
            _thumbnailImageProperty = new WProperty(typeof(string), thumbImage);
        }

        public VideoViewModel(VideoInfo videoInfo, string siteName, string utilName)
            : base(Consts.KEY_NAME, !string.IsNullOrEmpty(videoInfo.Title2) ? videoInfo.Title2 : videoInfo.Title)
        {
            VideoInfo = videoInfo;
			SiteName = siteName;
			SiteUtilName = utilName;

            _titleProperty = new WProperty(typeof(string), videoInfo.Title);
            _title2Property = new WProperty(typeof(string), videoInfo.Title2);
            _descriptionProperty = new WProperty(typeof(string), videoInfo.Description);
            _lengthProperty = new WProperty(typeof(string), videoInfo.Length);
			_airdateProperty = new WProperty(typeof(string), videoInfo.Airdate);
            _thumbnailImageProperty = new WProperty(typeof(string), videoInfo.ThumbnailImage);

			eventDelegator = OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(PropertyChangedDelegator).Assembly.FullName, typeof(PropertyChangedDelegator).FullName) as PropertyChangedDelegator;
			eventDelegator.InvokeTarget = new PropertyChangedExecutor()
			{
				InvokeHandler = (s, e) =>
				{
					if (e.PropertyName == "ThumbnailImage") ThumbnailImage = (s as VideoInfo).ThumbnailImage;
					else if (e.PropertyName == "Length") Length = (s as VideoInfo).Length;
				}
			};
			VideoInfo.PropertyChanged += eventDelegator.EventDelegate;
        }
    }
}
