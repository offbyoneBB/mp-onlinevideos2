using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UiComponents.Media.General;
using OnlineVideos.CrossDomain;

namespace OnlineVideos.MediaPortal2
{
    public class CategoryViewModel : ListItem
    {
		protected PropertyChangedDelegator eventDelegator = null;

        protected AbstractProperty _nameProperty;
        public AbstractProperty NameProperty { get { return _nameProperty; } }
        public string Name
        {
            get { return (string)_nameProperty.GetValue(); }
            set { _nameProperty.SetValue(value); }
        }

        protected AbstractProperty _descriptionProperty;
        public AbstractProperty DescriptionProperty { get { return _descriptionProperty; } }
        public string Description
        {
            get { return (string)_descriptionProperty.GetValue(); }
            set { _descriptionProperty.SetValue(value); }
        }

        protected AbstractProperty _thumbProperty;
        public AbstractProperty ThumbProperty { get { return _thumbProperty; } }
        public string Thumb
        {
            get { return (string)_thumbProperty.GetValue(); }
            set { _thumbProperty.SetValue(value); }
        }

        protected AbstractProperty _estimatedChildrenProperty;
        public AbstractProperty EstimatedChildrenProperty { get { return _estimatedChildrenProperty; } }
        public uint? EstimatedChildren
        {
            get { return (uint?)_estimatedChildrenProperty.GetValue(); }
            set { _estimatedChildrenProperty.SetValue(value); }
        }

        protected Category _category;
        public Category Category
        {
            get { return _category; }
        }

        public CategoryViewModel(Category category)
			: base(Consts.KEY_NAME, category.Name)
        {
            _category = category;

            _nameProperty = new WProperty(typeof(string), category.Name);
            _descriptionProperty = new WProperty(typeof(string), category.Description);
            _thumbProperty = new WProperty(typeof(string), null);
            _estimatedChildrenProperty = new WProperty(typeof(uint?), CalculateChildrenCount());

			if (Category is NextPageCategory)
			{
				Thumb = "NextPage.png";
			}
			else
			{
				eventDelegator = OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(PropertyChangedDelegator).Assembly.FullName, typeof(PropertyChangedDelegator).FullName) as PropertyChangedDelegator;
				eventDelegator.InvokeTarget = new PropertyChangedExecutor()
				{
					InvokeHandler = (s, e) =>
					{
						if (e.PropertyName == "ThumbnailImage") Thumb = (s as Category).ThumbnailImage;
					}
				};
				_category.PropertyChanged += eventDelegator.EventDelegate;
			}
        }

        uint? CalculateChildrenCount()
        {
            if (Category.HasSubCategories)
            {
                if (Category.SubCategories != null && Category.SubCategories.Count > 0) return (uint)Category.SubCategories.Count;
                return null;
            }
            else if (Category is Group)
            {
                Group group = Category as Group;
                if (group.Channels != null) return (uint)group.Channels.Count;
                else return 0;
            }
            else if (Category is RssLink)
            {
                return (Category as RssLink).EstimatedVideoCount;
            }
            return null;
        }
    }
}
