using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace OnlineVideos
{
    /// <summary>
    /// A common abstract base class for <see cref="Category"/> and <see cref="VideoInfo"/>.
    /// </summary>
    public abstract class SearchResultItem : MarshalByRefObject, INotifyPropertyChanged
    {
        object _other;
        protected string _thumb;
        protected string _thumbnailImage;

        [DataMember(Name = "desc", Order = 2, EmitDefaultValue = false)]
        [XmlAttribute("desc")]
        public string Description { get; set; }

        [DataMember(Name = "thumb", Order = 1, EmitDefaultValue = false)]
        [XmlAttribute("thumb")]
        public string Thumb { get { return _thumb; } set { _thumb = value; NotifyPropertyChanged("Thumb"); } }

        /// <summary>This property is set by the <see cref="Downloading.ImageDownloader"/> 
        /// to the path of the image file after downloading from <see cref="Thumb"/>.</summary>
        public string ThumbnailImage { get { return _thumbnailImage; } set { _thumbnailImage = value; NotifyPropertyChanged("ThumbnailImage"); } }

        /// <summary>This property is used by the <see cref="Downloading.ImageDownloader"/> 
        /// to resize the thumbnail after downloading from <see cref="Thumb"/> to a given aspect ratio (width/height).</summary>
        [XmlIgnore]
        public float? ImageForcedAspectRatio { get; set; }

        /// <summary>If you have additional data that you need to identify your object you can store it here.<br/>
        /// In order to make a <see cref="VideoInfo"/> work with Favorites, mark custom classes as [Serializable] and make them public.</summary>
        [XmlIgnore]
        public object Other 
		{ 
			get { return _other; }
			set 
			{
				if (_other != value)
				{
                    // unsubscribe from a previous notfier
                    var oldNotifier = _other as INotifyPropertyChanged;
                    if (oldNotifier != null) oldNotifier.PropertyChanged -= OnOtherPropertyChanged;

					_other = value;

                    // propagate changes in the Other object (if it supports INotifyPropertyChanged)
					var newNotifier = _other as INotifyPropertyChanged;
                    if (newNotifier != null) newNotifier.PropertyChanged += OnOtherPropertyChanged;
				}
			}
		}

        void OnOtherPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("Other");
        }

        #region MarshalByRefObject overrides

        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }

        #endregion

        #region INotifyPropertyChanged Member

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(string propertyName)
        {
            var propChanged = PropertyChanged;
            if (propChanged != null) 
                propChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
