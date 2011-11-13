using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace OnlineVideos.MediaPortal2
{
    public class VideoInfoViewModel : ListItem
    {
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

        protected AbstractProperty _thumbnailImageProperty;
        public AbstractProperty ThumbnailImageProperty { get { return _thumbnailImageProperty; } }
        public string ThumbnailImage
        {
            get { return (string)_thumbnailImageProperty.GetValue(); }
            set { _thumbnailImageProperty.SetValue(value); }
        }

        protected AbstractProperty _hasFocus;
        public AbstractProperty HasFocusProperty { get { return _hasFocus; } }
        public bool HasFocus
        {
            get { return (bool)_hasFocus.GetValue(); }
            set { _hasFocus.SetValue(value); }
        }
        
        protected VideoInfo _videoInfo;
        public VideoInfo VideoInfo
        {
            get { return _videoInfo; }
        }

        public VideoInfoViewModel(string title, string thumbImage)
        {
            _titleProperty = new WProperty(typeof(string), title);
            _thumbnailImageProperty = new WProperty(typeof(string), thumbImage);
            _hasFocus = new WProperty(typeof(bool), false);
        }

        public VideoInfoViewModel(VideoInfo videoInfo)
        {
            _videoInfo = videoInfo;

            _titleProperty = new WProperty(typeof(string), videoInfo.Title);
            _title2Property = new WProperty(typeof(string), videoInfo.Title2);
            _descriptionProperty = new WProperty(typeof(string), videoInfo.Description);
            _lengthProperty = new WProperty(typeof(string), videoInfo.Length);
            _thumbnailImageProperty = new WProperty(typeof(string), videoInfo.ThumbnailImage);
            _hasFocus = new WProperty(typeof(bool), false);

            _videoInfo.PropertyChanged += (sender, e) => { if (e.PropertyName == "ThumbnailImage") ThumbnailImage = VideoInfo.ThumbnailImage; };
        }
    }
}
