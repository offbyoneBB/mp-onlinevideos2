using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections;
using OnlineVideos.CrossDomain;

namespace Standalone.ViewModels
{
	public class Video : INotifyPropertyChanged, IDisposable
	{
		public Video(OnlineVideos.VideoInfo videoInfo, bool useTitle2 = false)
		{
			Model = videoInfo;

			videoInfo.CleanDescriptionAndTitle();

			Name = useTitle2 ? videoInfo.Title2 : videoInfo.Title;
			Description = videoInfo.Description;
			ThumbnailImage = videoInfo.ThumbnailImage;
			Length = videoInfo.Length;
			Airdate = videoInfo.Airdate;

			// we cannot attach directly to the PropertyChanged event of the Model as it is in another domain and PropertyChangedEventArgs is not serializable
			eventDelegator = OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(PropertyChangedDelegator).Assembly.FullName, typeof(PropertyChangedDelegator).FullName) as PropertyChangedDelegator;
			eventDelegator.InvokeTarget = new PropertyChangedExecutor() { InvokeHandler = ModelPropertyChanged };
			Model.PropertyChanged += eventDelegator.EventDelegate;
		}

		public OnlineVideos.VideoInfo Model { get; protected set; }

		public string Name { get; protected set; }
		public string Description { get; protected set; }
		public string ThumbnailImage { get; set; }
		public string Length { get; protected set; }
		public string Airdate { get; protected set; }

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
                        case "Length":
                            Length = Model.Length;
                            PropertyChanged(this, new PropertyChangedEventArgs("Length"));
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

	public static class VideoList
	{
		public static ListCollectionView GetVideosView(OnlineVideosMainWindow window, IList<OnlineVideos.VideoInfo> videoList, bool addNextPage, bool useTitle2 = false)
		{
			List<Video> convertedVideos = new List<Video>();
			foreach (OnlineVideos.VideoInfo video in videoList)
			{
				convertedVideos.Add(new Video(video, useTitle2));
			}

			if (addNextPage)
			{
				convertedVideos.Add(
					new Video(
						new OnlineVideos.VideoInfo()
						{
							Title = OnlineVideos.Translation.Instance.NextPage
						})
						{
							ThumbnailImage = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png")
						});
			}

			return new ListCollectionView(convertedVideos)
			{
				Filter = new Predicate<object>(v => (((Video)v).Name ?? "").ToLower().Contains(window.CurrentFilter))
			};
		}

		public static void AppendNextPageVideos(IEnumerable view, IList<OnlineVideos.VideoInfo> videoList, bool addNextPage, bool useTitle2 = false)
		{
			List<Video> sourceList = (view as ListCollectionView).SourceCollection as List<Video>;
			sourceList.InsertRange(sourceList.Count - 1, videoList.Select(s => new Video(s, useTitle2)).ToList());
			if (!addNextPage) sourceList.RemoveAt(sourceList.Count - 1);
			(view as ListCollectionView).Refresh();
		}
	}
}
