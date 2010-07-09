using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using System.Net;

namespace OnlineVideos.Sites
{
    public class DownloadedVideoUtil : SiteUtilBase, IFilter 
    {
        string lastSort = "date";

        // keep a reference of all Categories ever created and reuse them, to get them selected when returning to the category view
        Dictionary<string, Category> cachedCategories = new Dictionary<string, Category>();

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Category cat = null;
            // add a category for all files
            if (!cachedCategories.TryGetValue(Translation.All, out cat))
            {
                cat = new RssLink() { Name = Translation.All, Url = OnlineVideoSettings.Instance.DownloadDir };
                cachedCategories.Add(cat.Name, cat);
            }
            Settings.Categories.Add(cat);

            if (GUIOnlineVideos.currentDownloads.Count > 0)
            {
                // add a category for all downloads in progress
                if (!cachedCategories.TryGetValue(Translation.Downloading, out cat))
                {
                    cat = new Category() { Name = Translation.Downloading, Description = Translation.DownloadingDescription };
                    cachedCategories.Add(cat.Name, cat);
                }
                Settings.Categories.Add(cat);
            }

            foreach (string aDir in Directory.GetDirectories(OnlineVideoSettings.Instance.DownloadDir))
            {
                SiteUtilBase util = null;
                if (OnlineVideoSettings.Instance.SiteList.TryGetValue(Path.GetFileName(aDir), out util))
                {
                    SiteSettings aSite = util.Settings;
                    if (aSite.IsEnabled &&
                       (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.useAgeConfirmation || OnlineVideoSettings.Instance.ageHasBeenConfirmed))
                    {
                        if (!cachedCategories.TryGetValue(aSite.Name + " - " + Translation.DownloadedVideos, out cat))
                        {
                            cat = new RssLink();
                            cat.Name = aSite.Name + " - " + Translation.DownloadedVideos;
                            cat.Description = aSite.Description;
                            ((RssLink)cat).Url = aDir;
                            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + aSite.Name + ".png";
                            cachedCategories.Add(cat.Name, cat);
                        }
                        Settings.Categories.Add(cat);
                    }
                }
            }

            // need to always get the categories, because when adding new fav video from a new site, a removing the last one for a site, the categories must be refreshed 
            Settings.DynamicCategoriesDiscovered = false;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoList(category is RssLink ? (category as RssLink).Url : null, "*", category.Name == Translation.All);
        }

        List<VideoInfo> getVideoList(string path, string search, bool recursive)
        {
            List<VideoInfo> loVideoInfoList = new List<VideoInfo>();
            if (!(string.IsNullOrEmpty(path)))
            {
                FileInfo[] files = new DirectoryInfo(path).GetFiles(search, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                foreach (FileInfo file in files)
                {
                    if (isPossibleVideo(file.Name))
                    {
                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.VideoUrl = file.FullName;
                        loVideoInfo.ImageUrl = file.FullName.Substring(0, file.FullName.LastIndexOf(".")) + ".jpg";
                        loVideoInfo.Length = file.LastWriteTime.ToString("g", OnlineVideoSettings.Instance.MediaPortalLocale);
                        loVideoInfo.Title = MediaPortal.Util.Utils.GetFilename(file.Name);
                        loVideoInfo.Description = string.Format("{0} MB", (file.Length / 1024 / 1024).ToString("N0"));
                        loVideoInfo.Other = file;
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }

                switch (lastSort)
                {
                    case "name":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2) 
                        { 
                            return v1.Title.CompareTo(v2.Title); 
                        });
                        break;
                    case "date":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2) 
                        {
                            return (v2.Other as FileInfo).LastWriteTime.CompareTo((v1.Other as FileInfo).LastWriteTime); 
                        });
                        break;
                    case "size":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2)
                        {
                            return (v2.Other as FileInfo).Length.CompareTo((v1.Other as FileInfo).Length);
                        });
                        break;
                }
            }
            else
            {
                lock (GUIOnlineVideos.currentDownloads)
                {
                    foreach (DownloadInfo di in GUIOnlineVideos.currentDownloads.Values)
                    {
                        string progressInfo = (di.PercentComplete != 0 || di.KbTotal != 0 || di.KbDownloaded != 0) ?
                            string.Format(" | {0}% / {1} KB - {2} KB/sec", di.PercentComplete, di.KbTotal > 0 ? di.KbTotal.ToString("n0") : di.KbDownloaded.ToString("n0"), (int)(di.KbDownloaded / (DateTime.Now - di.Start).TotalSeconds)) : "";

                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.Title = di.Title;                        
                        loVideoInfo.ImageUrl = di.ThumbFile;
                        loVideoInfo.Length = di.Start.ToString("HH:mm:ss") + progressInfo;
                        loVideoInfo.Description = string.Format("{0}\n{1}", di.Url, di.LocalFile, progressInfo);
                        loVideoInfo.Other = di;
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }
            }
            return loVideoInfoList;
        }

        public override List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<string> options = new List<string>();
            if (selectedCategory is RssLink)
            {
                options.Add(Translation.Delete);
            }
            else
            {
                options.Add(GUILocalizeStrings.Get(222));
            }
            return options;
        }

        public override bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice)
        {
            if (choice == Translation.Delete)
            {
                if (System.IO.File.Exists(selectedItem.ImageUrl)) System.IO.File.Delete(selectedItem.ImageUrl);
                if (System.IO.File.Exists(selectedItem.VideoUrl)) System.IO.File.Delete(selectedItem.VideoUrl);
                return true;
            }
            else if (choice == GUILocalizeStrings.Get(222))
            {
                ((IDownloader)(selectedItem.Other as DownloadInfo).Downloader).CancelAsync();
            }
            return false;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            query = query.Replace(' ', '*');
            if (!query.StartsWith("*")) query = "*" + query;
            if (!query.EndsWith("*")) query += "*";
            return getVideoList(OnlineVideoSettings.Instance.DownloadDir, query, true);
        }

        #endregion

        #region IFilter Member

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            lastSort = orderBy;
            return getVideoList(category);
        }

        public List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame)
        {
            lastSort = orderBy;
            return Search(query);
        }

        public List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            return null;
        }

        public List<int> getResultSteps()
        {
            return new List<int>();
        }

        public Dictionary<string, string> getOrderbyList()
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add(GUILocalizeStrings.Get(104), "date");
            options.Add(GUILocalizeStrings.Get(365), "name");
            options.Add(GUILocalizeStrings.Get(105), "size");
            return options;
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return new Dictionary<string,string>();
        }

        #endregion
    }
}
