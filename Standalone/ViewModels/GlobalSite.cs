using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;

namespace Standalone.ViewModels
{
    public class GlobalSite : OnlineVideos.ISearchResultItem, INotifyPropertyChanged
    {
        public GlobalSite(OnlineVideos.OnlineVideosWebservice.Site site)
        {
            Model = site;

            Owner = Model.Owner_FK.Substring(0, Model.Owner_FK.IndexOf('@')).Replace('.', ' ').Replace('_', ' ');
            Language = Util.GetLocalizedLanguageDisplayName(site.Language);
        }

        public OnlineVideos.OnlineVideosWebservice.Site Model { get; protected set; }

        public string Name { get { return Model.Name; } }
        public System.DateTime LastUpdated { get { return Model.LastUpdated; } }
        public OnlineVideos.OnlineVideosWebservice.SiteState State { get { return Model.State; } }
        public bool IsAdult { get { return Model.IsAdult; } }

        public string Owner { get; protected set; }
        public string Language { get; protected set; }

        public string Description { get { return Model.Description; } set { Model.Description = value; } }
        public object Other { get { return Model; } set { Model = value as OnlineVideos.OnlineVideosWebservice.Site ?? Model; } }
        protected string _ThumbnailImage = "";
        public string ThumbnailImage 
        { 
            get { return _ThumbnailImage; } 
            set 
            {
                if (_ThumbnailImage != value)
                {
                    _ThumbnailImage = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("ThumbnailImage"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public static class GlobalSiteList
    {
        public static ListCollectionView GetSitesView(OnlineVideosMainWindow window)
        {
            return new ListCollectionView(OnlineVideos.Sites.Updater.OnlineSites.Select(s => new GlobalSite(s)).ToList())
            {
                Filter = new Predicate<object>(site => (((GlobalSite)site).Name ?? "").ToLower().Contains(window.CurrentFilter))
            };
        }
    }
}