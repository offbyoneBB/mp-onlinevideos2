using System;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace Standalone.ViewModels
{
    public class GlobalSite : OnlineVideos.SearchResultItem
    {
        public GlobalSite(OnlineVideos.OnlineVideosWebservice.Site site)
        {
            Model = site;
            Other = Model;

            Owner = Model.Owner_FK.Substring(0, Model.Owner_FK.IndexOf('@')).Replace('.', ' ').Replace('_', ' ');
            Language = Util.GetLocalizedLanguageDisplayName(site.Language);

            ThumbnailImage = Path.Combine(OnlineVideos.OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + site.Name + ".png");
            Thumb = "http://onlinevideos.nocrosshair.de/Icons/" + site.Name + ".png";
        }

        public OnlineVideos.OnlineVideosWebservice.Site Model { get; protected set; }

        public string Name { get { return Model.Name; } }
        public DateTime LastUpdated { get { return Model.LastUpdated; } }
        public OnlineVideos.OnlineVideosWebservice.SiteState State { get { return Model.State; } }
        public bool IsAdult { get { return Model.IsAdult; } }
        public string Owner { get; protected set; }
        public string Language { get; protected set; }
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