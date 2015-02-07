using System;
using System.Collections.Generic;
using SQLite.NET;
using MediaPortal.Configuration;
using MediaPortal.Database;

namespace OnlineVideos.MediaPortal1
{
    public class FavoritesDatabase : MarshalByRefObject, IFavoritesDatabase
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
            try
            {
                m_db = new SQLiteClient(Config.GetFile(Config.Dir.Database, "OnlineVideoDatabase.db3"));
                DatabaseUtility.SetPragmas(m_db);
                DatabaseUtility.AddTable(m_db, "FAVORITE_VIDEOS", "CREATE TABLE FAVORITE_VIDEOS(VDO_ID integer primary key autoincrement,VDO_NM text,VDO_URL text,VDO_DESC text,VDO_TAGS text,VDO_LENGTH text,VDO_OTHER_NFO text,VDO_IMG_URL text,VDO_SITE_ID text)\n");
                DatabaseUtility.AddTable(m_db, "FAVORITE_Categories", "CREATE TABLE FAVORITE_Categories(CAT_ID integer primary key autoincrement,CAT_Name text,CAT_Desc text,CAT_ThumbUrl text,CAT_Hierarchy text,CAT_SITE_ID text)\n");
				DatabaseUtility.AddTable(m_db, "PREFERRED_LAYOUT", "CREATE TABLE PREFERRED_LAYOUT(Site_Name text, Category_Hierarchy text, Layout integer, PRIMARY KEY (Site_Name, Category_Hierarchy) ON CONFLICT REPLACE)\n");
            }
            catch (SQLiteException ex)
            {
                Log.Instance.Error("database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
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

        public List<KeyValuePair<string,uint>> GetSiteIds()
        {
            string lsSQL = @"select distinct VDO_SITE_ID, max(NumVideos) as NumVideos from
                            (
                            select VDO_SITE_ID, count(*) as NumVideos from Favorite_Videos group by VDO_SITE_ID
                            UNION
                            select CAT_SITE_ID, 0 from Favorite_Categories
                            )
                            group by VDO_SITE_ID";
            SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
            List<KeyValuePair<string, uint>> siteIdList = new List<KeyValuePair<string,uint>>();
            for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
            {
                siteIdList.Add(new KeyValuePair<string,uint>(DatabaseUtility.Get(loResultSet, iRow, "VDO_SITE_ID"), (uint)DatabaseUtility.GetAsInt(loResultSet, iRow, "NumVideos")));
            }
            return siteIdList;
        }

        public bool AddFavoriteVideo(VideoInfo foVideo, string titleFromUtil, string siteName)
        {
			DatabaseUtility.RemoveInvalidChars(ref siteName);
            string title = string.IsNullOrEmpty(titleFromUtil) ? "" : DatabaseUtility.RemoveInvalidChars(titleFromUtil);
            string desc = string.IsNullOrEmpty(foVideo.Description) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Description);
            string thumb = string.IsNullOrEmpty(foVideo.Thumb) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Thumb);
            string url = string.IsNullOrEmpty(foVideo.VideoUrl) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.VideoUrl);
            string length = string.IsNullOrEmpty(foVideo.Length) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Length);
            string airdate = string.IsNullOrEmpty(foVideo.Airdate) ? "" : DatabaseUtility.RemoveInvalidChars(foVideo.Airdate);
            string other = DatabaseUtility.RemoveInvalidChars(foVideo.GetOtherAsString());

            Log.Instance.Info("inserting favorite on site '{4}' with title: '{0}', desc: '{1}', thumb: '{2}', url: '{3}'", title, desc, thumb, url, siteName);

            //check if the video is already in the favorite list
            string lsSQL = string.Format("select VDO_ID from FAVORITE_VIDEOS where VDO_SITE_ID='{0}' AND VDO_URL='{1}' and VDO_OTHER_NFO='{2}'", siteName, url, other);
            if (m_db.Execute(lsSQL).Rows.Count > 0)
            {
                Log.Instance.Info("Favorite Video '{0}' already in database", title);
                return false;
            }

            lsSQL =
                string.Format(
                    "insert into FAVORITE_VIDEOS(VDO_NM,VDO_URL,VDO_DESC,VDO_TAGS,VDO_LENGTH,VDO_OTHER_NFO,VDO_IMG_URL,VDO_SITE_ID)VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')",
                    title, url, desc, airdate, length, other, thumb, siteName);
            m_db.Execute(lsSQL);
            if (m_db.ChangedRows() > 0)
            {
                Log.Instance.Info("Favorite '{0}' inserted successfully into database", foVideo.Title);
                return true;
            }
            else
            {
                Log.Instance.Warn("Favorite '{0}' failed to insert into database", foVideo.Title);
                return false;
            }
        }

        public bool RemoveFavoriteVideo(FavoriteVideoInfo foVideo)
        {
            String lsSQL = string.Format("delete from FAVORITE_VIDEOS where VDO_ID='{0}' ", foVideo.Id);
            m_db.Execute(lsSQL);
            return m_db.ChangedRows() > 0;
        }

        public bool RemoveAllFavoriteVideos(string siteName)
        {
			DatabaseUtility.RemoveInvalidChars(ref siteName);
            string sql = "delete from FAVORITE_VIDEOS";
            if (!string.IsNullOrEmpty(siteName)) sql += string.Format(" where VDO_SITE_ID='{0}'", siteName);
            m_db.Execute(sql);
            return m_db.ChangedRows() > 0;
        }

        public List<VideoInfo> GetFavoriteVideos(string siteName, string fsQuery)
        {
            string lsSQL = "select * from favorite_videos";
            if (!string.IsNullOrEmpty(siteName))
            {
				DatabaseUtility.RemoveInvalidChars(ref siteName);
                lsSQL += string.Format(" where VDO_SITE_ID='{0}'", siteName);
            }
            if (!string.IsNullOrEmpty(fsQuery))
            {
                if (string.IsNullOrEmpty(siteName)) 
                    lsSQL += string.Format(" where VDO_NM like '%{0}%' or VDO_DESC like '%{0}%'", fsQuery);
                else
                    lsSQL += string.Format(" and (VDO_NM like '%{0}%' or VDO_DESC like '%{0}%')", fsQuery);
            }

            SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
            List<VideoInfo> loFavoriteList = new List<VideoInfo>();
            if (loResultSet.Rows.Count == 0) return loFavoriteList;

            for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
            {
                var video = CrossDomain.OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(FavoriteVideoInfo).Assembly.FullName, typeof(FavoriteVideoInfo).FullName) as FavoriteVideoInfo;
                video.Description = DatabaseUtility.Get(loResultSet, iRow, "VDO_DESC");
                video.Thumb = DatabaseUtility.Get(loResultSet, iRow, "VDO_IMG_URL");
                video.Length = DatabaseUtility.Get(loResultSet, iRow, "VDO_LENGTH");
                video.Airdate = DatabaseUtility.Get(loResultSet, iRow, "VDO_TAGS");
                video.Title = DatabaseUtility.Get(loResultSet, iRow, "VDO_NM");
                video.VideoUrl = DatabaseUtility.Get(loResultSet, iRow, "VDO_URL");
                video.SetOtherFromString(DatabaseUtility.Get(loResultSet, iRow, "VDO_OTHER_NFO"));
                video.Id = DatabaseUtility.GetAsInt(loResultSet, iRow, "VDO_ID");
                video.SiteName = DatabaseUtility.Get(loResultSet, iRow, "VDO_SITE_ID");
                Log.Instance.Debug("Pulled '{0}' out of the database", video.Title);

                if (OnlineVideoSettings.Instance.SiteUtilsList.ContainsKey(video.SiteName))
                {
                    SiteSettings aSite = OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName].Settings;
                    if (aSite.IsEnabled && (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                    {
                        loFavoriteList.Add(video);
                    }
                }
            }
            return loFavoriteList;
        }

        public bool AddFavoriteCategory(Category cat, string siteName)
        {
			DatabaseUtility.RemoveInvalidChars(ref siteName);
            string categoryHierarchyName = EscapeString(cat.RecursiveName("|"));

            //check if the category is already in the favorite list
            if (m_db.Execute(string.Format("select CAT_ID from FAVORITE_Categories where CAT_Hierarchy='{0}' AND CAT_SITE_ID='{1}'", categoryHierarchyName, siteName)).Rows.Count > 0)
            {
                Log.Instance.Info("Favorite Category {0} already in database", cat.Name);
                return true;
            }

            Log.Instance.Info("inserting favorite category on site {0} with name: {1}, desc: {2}, image: {3}", 
                siteName, cat.Name, cat.Description, cat.Thumb, siteName);

            string lsSQL =
                string.Format(
                    "insert into FAVORITE_Categories(CAT_Name,CAT_Desc,CAT_ThumbUrl,CAT_Hierarchy,CAT_SITE_ID)VALUES('{0}','{1}','{2}','{3}','{4}')",
                    DatabaseUtility.RemoveInvalidChars(cat.Name), cat.Description == null ? "" : DatabaseUtility.RemoveInvalidChars(cat.Description), cat.Thumb, categoryHierarchyName, siteName);
            m_db.Execute(lsSQL);
            if (m_db.ChangedRows() > 0)
            {
                Log.Instance.Info("Favorite Category {0} inserted successfully into database", cat.Name);
                return true;
            }
            else
            {
                Log.Instance.Warn("Favorite Category {0} failed to insert into database", cat.Name);
                return false;
            }
        }

        public List<Category> GetFavoriteCategories(string siteName)
        {
			DatabaseUtility.RemoveInvalidChars(ref siteName);
            List<Category> results = new List<Category>();
            SQLiteResultSet resultSet = m_db.Execute(string.Format("select * from Favorite_Categories where CAT_SITE_ID = '{0}'", siteName));
            for (int iRow = 0; iRow < resultSet.Rows.Count; iRow++)
            {
                results.Add(
                    new RssLink() { 
                        Name = DatabaseUtility.Get(resultSet, iRow, "CAT_Name"), 
                        Description = DatabaseUtility.Get(resultSet, iRow, "CAT_Desc"),
                        Thumb = DatabaseUtility.Get(resultSet, iRow, "CAT_ThumbUrl"),
                        Url = DatabaseUtility.Get(resultSet, iRow, "CAT_ID"),
                        Other = DatabaseUtility.Get(resultSet, iRow, "CAT_Hierarchy")
                    });
            }
            return results;
        }

        public List<string> GetFavoriteCategoriesNames(string siteName)
        {
			DatabaseUtility.RemoveInvalidChars(ref siteName);
            List<string> results = new List<string>();
            SQLiteResultSet resultSet = m_db.Execute(string.Format("select CAT_Hierarchy from Favorite_Categories where CAT_SITE_ID = '{0}'", siteName));
            for (int iRow = 0; iRow < resultSet.Rows.Count; iRow++)
            {
                results.Add(DatabaseUtility.Get(resultSet, iRow, "CAT_Hierarchy"));
            }
            return results;
        }

        public bool RemoveFavoriteCategory(Category cat)
        {
            String lsSQL = string.Format("delete from Favorite_Categories where CAT_ID = '{0}'", (cat as RssLink).Url);
            m_db.Execute(lsSQL);
            return m_db.ChangedRows() > 0;
        }

        public bool RemoveFavoriteCategory(string siteName, string recursiveCategoryName)
        {
			DatabaseUtility.RemoveInvalidChars(ref siteName);
            String lsSQL = string.Format("delete from Favorite_Categories where CAT_Hierarchy='{0}' AND CAT_SITE_ID='{1}'", recursiveCategoryName, siteName);
            m_db.Execute(lsSQL);
            return m_db.ChangedRows() > 0;
        }

        string EscapeString(string input)
        {
            return input.Replace("'", "''");
        }

		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion

		public bool SetPreferredLayout(string siteName, Category cat, int Layout)
		{
			try
			{
				if (string.IsNullOrEmpty(siteName)) return false;
				DatabaseUtility.RemoveInvalidChars(ref siteName);
				string categoryHierarchyName = cat != null ? EscapeString(DatabaseUtility.RemoveInvalidChars(cat.RecursiveName("|"))) : "";
				m_db.Execute(string.Format("insert into PREFERRED_LAYOUT(Site_Name, Category_Hierarchy, Layout) VALUES ('{0}','{1}',{2})", siteName, categoryHierarchyName, Layout));
				return m_db.ChangedRows() > 0;
			}
			catch (Exception ex)
			{
				Log.Instance.Warn("Exception storing preferred Layout in DB: {0}", ex.ToString());
				return false;
			}
		}

		public MediaPortal.GUI.Library.GUIFacadeControl.Layout? GetPreferredLayout(string siteName, Category cat)
		{
			try
			{
				if (string.IsNullOrEmpty(siteName)) return null;
				DatabaseUtility.RemoveInvalidChars(ref siteName);
				string categoryHierarchyName = cat != null ? EscapeString(DatabaseUtility.RemoveInvalidChars(cat.RecursiveName("|"))) : "";
				if (!string.IsNullOrEmpty(categoryHierarchyName))
				{
					var resultSet = m_db.Execute(string.Format("SELECT Layout FROM PREFERRED_LAYOUT WHERE Site_Name = '{0}' AND Category_Hierarchy = '{1}'", siteName, categoryHierarchyName));
					if (resultSet.Rows.Count > 0)
					{
						return (MediaPortal.GUI.Library.GUIFacadeControl.Layout)int.Parse(DatabaseUtility.Get(resultSet, 0, "Layout"));
					}
				}
				return null;
			}
			catch (Exception ex)
			{
				Log.Instance.Warn("Exception getting preferred Layout from DB: {0}", ex.ToString());
				return null;
			}
		}
    }
}
