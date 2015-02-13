using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.ComponentModel;
using OnlineVideos.CrossDomain;

namespace Standalone.ViewModels
{
	public class Category : INotifyPropertyChanged, IDisposable
	{
		public Category(OnlineVideos.Category category)
		{
			Model = category;

			Name = category.Name;
			Description = category.Description;
			ThumbnailImage = category.ThumbnailImage;
			CategoryPath = category.RecursiveName();
			if (category is OnlineVideos.RssLink) EstimatedVideoCount = (category as OnlineVideos.RssLink).EstimatedVideoCount;

			// we cannot attach directly to the PropertyChanged event of the Model as it is in another domain and PropertyChangedEventArgs is not serializable
			eventDelegator = OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(PropertyChangedDelegator).Assembly.FullName, typeof(PropertyChangedDelegator).FullName) as PropertyChangedDelegator;
			eventDelegator.InvokeTarget = new PropertyChangedExecutor() { InvokeHandler = ModelPropertyChanged };
			Model.PropertyChanged += eventDelegator.EventDelegate;
		}

		public OnlineVideos.Category Model { get; protected set; }

		public string Name { get; protected set; }
		public string Description { get; protected set; }
		public string ThumbnailImage { get; protected set; }
		public string CategoryPath { get; protected set; }
		public uint? EstimatedVideoCount { get; protected set; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected PropertyChangedDelegator eventDelegator = null;
		protected void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				if (PropertyChanged != null)
				{
					switch (e.PropertyName)
					{
						case "ThumbnailImage":
							ThumbnailImage = Model.ThumbnailImage;
							PropertyChanged(this, new PropertyChangedEventArgs("ThumbnailImage"));
							break;
					}
				}
			}), System.Windows.Threading.DispatcherPriority.Input);
		}

		public void Dispose()
		{
			Model.PropertyChanged -= eventDelegator.EventDelegate;
		}
	}

	public static class CategoryList
	{
		public static ListCollectionView GetCategoriesView(OnlineVideosMainWindow window, IList<OnlineVideos.Category> categories, string preselectedCategoryName = null)
		{
			int? indexToSelect = null;
			List<Category> convertedCategories = new List<Category>();
			int i = 0;
			foreach (var c in categories)
			{
				if (preselectedCategoryName != null && c.Name == preselectedCategoryName) indexToSelect = i;
				convertedCategories.Add(new Category(c));
				i++;
			}
			ListCollectionView view = new ListCollectionView(convertedCategories)
			{
				Filter = new Predicate<object>(cat => (((Category)cat).Name ?? "").ToLower().Contains(window.CurrentFilter))
			};
			if (indexToSelect != null) view.MoveCurrentToPosition(indexToSelect.Value);
			return view;
		}
	}
}
