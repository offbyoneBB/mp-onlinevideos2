using System;
using System.Collections.Generic;
using SQLite.NET;
using MediaPortal.Configuration;
using MediaPortal.Database;

namespace OnlineVideos.Database
{
    public class FavoritesDatabase
    {
        private SQLiteClient m_db;

        private static FavoritesDatabase _Instance;
        
        public static FavoritesDatabase Instance
        {
            get
            {
                if (_Instance == null) _Instance = new FavoritesDatabase();
                return _Instance;
            }
        }        

        private FavoritesDatabase()
        {
            bool dbExists;
            try
            {
                // Open database
                try
                {
                    System.IO.Directory.CreateDirectory("database");
                }
                catch (Exception) { }
                dbExists = System.IO.File.Exists(Config.GetFile(Config.Dir.Database, "OnlineVideoDatabase.db3"));
                m_db = new SQLiteClient(Config.GetFile(Config.Dir.Database, "OnlineVideoDatabase.db3"));

                MediaPortal.Database.DatabaseUtility.SetPragmas(m_db);

                if (!dbExists)
                {
                    m_db.Execute("CREATE TABLE FAVORITE_VIDEOS(VDO_ID integer primary key autoincrement,VDO_NM text,VDO_URL text,VDO_DESC text,VDO_TAGS text,VDO_LENGTH text,VDO_OTHER_NFO text,VDO_IMG_URL text,VDO_SITE_ID text)\n");
                }
            }
            catch (SQLiteException ex)
            {
                Log.Error("database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            }
        }                

        public void Dispose()
        {
            if (m_db != null)
            {
                m_db.Close();
                m_db.Dispose();
                m_db = null;
            }
        }

        public string[] getSiteIDs()
        {
            string lsSQL = "select distinct VDO_SITE_ID from favorite_videos";
            SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
            string[] siteIdList = new string[loResultSet.Rows.Count];
            for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
            {
                siteIdList[iRow] = DatabaseUtility.Get(loResultSet, iRow, "VDO_SITE_ID");

            }
            return siteIdList;
        }

        public bool addFavoriteVideo(VideoInfo foVideo, string titleFromUtil, string siteName)
        {
            //check if the video is already in the favorite list
            //lsSQL = string.Format("select SONG_ID from FAVORITE_VIDEOS where SONG_ID='{0}' AND COUNTRY='{1}' and FAVORITE_ID=''", foVideo.songId, foVideo.countryId, lsFavID);
            //loResultSet = m_db.Execute(lsSQL);
            //if (loResultSet.Rows.Count > 0)
            //{
            //    return false;
            //}
            Log.Info("inserting favorite on site {4} with title: {0}, desc: {1}, image: {2}, url: {3}", foVideo.Title, foVideo.Description, foVideo.ImageUrl, foVideo.VideoUrl, siteName);

            string title = string.IsNullOrEmpty(titleFromUtil) ? "" : DatabaseUtility.RemoveInvalidChars(titleFromUtil);
            string desc = string.IsNullOrEmpty(foVideo.Description) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Description);
            string thumb = string.IsNullOrEmpty(foVideo.ImageUrl) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.ImageUrl);
            string url = string.IsNullOrEmpty(foVideo.VideoUrl) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.VideoUrl);
            string length = string.IsNullOrEmpty(foVideo.Length) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Length);
            string other = foVideo.Other == null ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Other.ToString());

            string lsSQL =
                string.Format(
                    "insert into FAVORITE_VIDEOS(VDO_NM,VDO_URL,VDO_DESC,VDO_TAGS,VDO_LENGTH,VDO_OTHER_NFO,VDO_IMG_URL,VDO_SITE_ID)VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')",
                    title, url, desc, "", length, other, thumb, siteName);
            m_db.Execute(lsSQL);
            if (m_db.ChangedRows() > 0)
            {
                Log.Info("Favorite {0} inserted successfully into database", foVideo.Title);
                return true;
            }
            else
            {
                Log.Warn("Favorite {0} failed to insert into database", foVideo.Title);
                return false;
            }
        }

        public bool removeFavoriteVideo(VideoInfo foVideo)
        {
            String lsSQL = string.Format("delete from FAVORITE_VIDEOS where VDO_ID='{0}' ", foVideo.Id);
            m_db.Execute(lsSQL);
            if (m_db.ChangedRows() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<VideoInfo> getFavoriteVideos(string fsSiteId, string fsQuery)
        {
            string lsSQL = "select * from favorite_videos";
            if (!string.IsNullOrEmpty(fsSiteId))
            {
                lsSQL += string.Format(" where VDO_SITE_ID='{0}'", fsSiteId);
            }
            if (!string.IsNullOrEmpty(fsQuery))
            {
                if (string.IsNullOrEmpty(fsSiteId)) 
                    lsSQL += string.Format(" where VDO_NM like '%{0}%' or VDO_DESC like '%{0}%'", fsQuery);
                else
                    lsSQL += string.Format(" and (VDO_NM like '%{0}%' or VDO_DESC like '%{0}%')", fsQuery);
            }

            SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
            List<VideoInfo> loFavoriteList = new List<VideoInfo>();
            if (loResultSet.Rows.Count == 0) return loFavoriteList;

            for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
            {
                VideoInfo video = new VideoInfo();
                video.Description = DatabaseUtility.Get(loResultSet, iRow, "VDO_DESC");
                video.ImageUrl = DatabaseUtility.Get(loResultSet, iRow, "VDO_IMG_URL");
                video.Length = DatabaseUtility.Get(loResultSet, iRow, "VDO_LENGTH");
                video.Title = DatabaseUtility.Get(loResultSet, iRow, "VDO_NM");
                video.VideoUrl = DatabaseUtility.Get(loResultSet, iRow, "VDO_URL");
                video.Other = DatabaseUtility.Get(loResultSet, iRow, "VDO_OTHER_NFO");
                video.Id = DatabaseUtility.GetAsInt(loResultSet, iRow, "VDO_ID");
                video.SiteName = DatabaseUtility.Get(loResultSet, iRow, "VDO_SITE_ID");
                Log.Debug("Pulled {0} out of the database", video.Title);

                if (OnlineVideoSettings.Instance.SiteList.ContainsKey(video.SiteName))
                {
                    SiteSettings aSite = OnlineVideoSettings.Instance.SiteList[video.SiteName].Settings;
                    if (aSite.IsEnabled && (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.useAgeConfirmation || OnlineVideoSettings.Instance.ageHasBeenConfirmed))
                    {
                        loFavoriteList.Add(video);
                    }
                }
            }
            return loFavoriteList;
        }     
    }
}
