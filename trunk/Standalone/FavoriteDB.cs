using OnlineVideos;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;

namespace Standalone
{
    public class FavoriteDB : MarshalByRefObject, IFavoritesDatabase
    {
        string dbFilePath;
        DbProviderFactory factory;

        public FavoriteDB(string dbFilePath)
        {
            this.dbFilePath = dbFilePath;    
        }

        public bool Init()
        {
            try
            {
                this.factory = DbProviderFactories.GetFactory("System.Data.SQLite");
                using (var cnn = OpenConnection())
                {
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA encoding = \"UTF-8\"";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS FAVORITE_VIDEOS(VDO_ID integer primary key autoincrement,VDO_NM text,VDO_URL text,VDO_DESC text,VDO_TAGS text,VDO_LENGTH text,VDO_OTHER_NFO text,VDO_IMG_URL text,VDO_SITE_ID text)";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS FAVORITE_Categories(CAT_ID integer primary key autoincrement,CAT_Name text,CAT_Desc text,CAT_ThumbUrl text,CAT_Hierarchy text,CAT_SITE_ID text)";
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Favorite Database initialize error: {0}", ex.ToString());
                return false;
            }
        }

        SQLiteConnection OpenConnection()
        {
            var connection = factory.CreateConnection() as SQLiteConnection;
            connection.ConnectionString = "Data Source=" + dbFilePath;
            connection.Open();
            return connection;
        }

        string RemoveInvalidChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "unknown"; // compatibility with MP1
            return text.Replace("'", "''").Trim();
        }

        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion

        public List<KeyValuePair<string, uint>> GetSiteIds()
        {
            List<KeyValuePair<string, uint>> siteIdList = new List<KeyValuePair<string, uint>>();

            string sql = @"select distinct VDO_SITE_ID, max(NumVideos) as NumVideos from
                            (
                            select VDO_SITE_ID, count(*) as NumVideos from Favorite_Videos group by VDO_SITE_ID
                            UNION
                            select CAT_SITE_ID, 0 from Favorite_Categories
                            )
                            group by VDO_SITE_ID";

            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using(var reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            siteIdList.Add(new KeyValuePair<string, uint>(
                                reader.GetString(reader.GetOrdinal("VDO_SITE_ID")), 
                                (uint)reader.GetInt64(reader.GetOrdinal("NumVideos"))));
                        }
                    }
                }
            }

            return siteIdList;
        }

        public bool AddFavoriteVideo(VideoInfo video, string titleFromUtil, string siteName)
        {
            siteName = RemoveInvalidChars(siteName);
            string title = string.IsNullOrEmpty(titleFromUtil) ? "" : RemoveInvalidChars(titleFromUtil);
            string desc = string.IsNullOrEmpty(video.Description) ? "" : RemoveInvalidChars(video.Description);
            string thumb = string.IsNullOrEmpty(video.Thumb) ? "" : RemoveInvalidChars(video.Thumb);
            string url = string.IsNullOrEmpty(video.VideoUrl) ? "" : RemoveInvalidChars(video.VideoUrl);
            string length = string.IsNullOrEmpty(video.Length) ? "" : RemoveInvalidChars(video.Length);
            string airdate = string.IsNullOrEmpty(video.Airdate) ? "" : RemoveInvalidChars(video.Airdate);
            string other = RemoveInvalidChars(video.GetOtherAsString());

            Log.Info("inserting favorite on site '{4}' with title: '{0}', desc: '{1}', thumb: '{2}', url: '{3}'", title, desc, thumb, url, siteName);

            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    //check if the video is already in the favorite list
                    string sql = string.Format("select VDO_ID from FAVORITE_VIDEOS where VDO_SITE_ID='{0}' AND VDO_URL='{1}' and VDO_OTHER_NFO='{2}'", siteName, url, other);
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            Log.Info("Favorite Video '{0}' already in database", title);
                            return false;
                        }
                    }

                    sql = string.Format("insert into FAVORITE_VIDEOS(VDO_NM,VDO_URL,VDO_DESC,VDO_TAGS,VDO_LENGTH,VDO_OTHER_NFO,VDO_IMG_URL,VDO_SITE_ID)VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')",
                            title, url, desc, airdate, length, other, thumb, siteName);
                    cmd.CommandText = sql;
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        Log.Info("Favorite '{0}' inserted successfully into database", video.Title);
                        return true;
                    }
                    else
                    {
                        Log.Warn("Favorite '{0}' failed to insert into database", video.Title);
                        return false;
                    }
                }
            }
        }

        public bool RemoveFavoriteVideo(FavoriteDbVideoInfo video)
        {
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    string sql = string.Format("delete from FAVORITE_VIDEOS where VDO_ID='{0}' ", video.Id);
                    cmd.CommandText = sql;
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool RemoveAllFavoriteVideos(string siteId)
        {
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    string sql = "delete from FAVORITE_VIDEOS";
                    if (!string.IsNullOrEmpty(siteId)) sql += string.Format(" where VDO_SITE_ID='{0}'", RemoveInvalidChars(siteId));
                    cmd.CommandText = sql;
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<VideoInfo> GetFavoriteVideos(string siteName, string query)
        {
            string sql = "select * from favorite_videos";
            if (!string.IsNullOrEmpty(siteName))
            {
                siteName = RemoveInvalidChars(siteName);
                sql += string.Format(" where VDO_SITE_ID='{0}'", siteName);
            }
            if (!string.IsNullOrEmpty(query))
            {
                if (string.IsNullOrEmpty(siteName))
                    sql += string.Format(" where VDO_NM like '%{0}%' or VDO_DESC like '%{0}%'", query);
                else
                    sql += string.Format(" and (VDO_NM like '%{0}%' or VDO_DESC like '%{0}%')", query);
            }

            List<VideoInfo> results = new List<VideoInfo>();
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var video = OnlineVideos.CrossDomain.OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(FavoriteDbVideoInfo).Assembly.FullName, typeof(FavoriteDbVideoInfo).FullName) as FavoriteDbVideoInfo;
                            video.Description = reader.GetString(reader.GetOrdinal("VDO_DESC"));
                            video.Thumb = reader.GetString(reader.GetOrdinal("VDO_IMG_URL"));
                            video.Length = reader.GetString(reader.GetOrdinal("VDO_LENGTH"));
                            video.Airdate = reader.GetString(reader.GetOrdinal("VDO_TAGS"));
                            video.Title = reader.GetString(reader.GetOrdinal("VDO_NM"));
                            video.VideoUrl = reader.GetString(reader.GetOrdinal("VDO_URL"));
                            video.SetOtherFromString(reader.GetString(reader.GetOrdinal("VDO_OTHER_NFO")));
                            video.Id = reader.GetInt32(reader.GetOrdinal("VDO_ID"));
                            video.SiteName = reader.GetString(reader.GetOrdinal("VDO_SITE_ID"));
                            Log.Debug("Pulled '{0}' out of the database", video.Title);

                            if (OnlineVideoSettings.Instance.SiteUtilsList.ContainsKey(video.SiteName))
                            {
                                SiteSettings aSite = OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName].Settings;
                                if (aSite.IsEnabled && (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                                {
                                    results.Add(video);
                                }
                            }
                        }
                    }
                }
            }
            return results;
        }

        public List<FavoriteDbCategory> GetFavoriteCategories(string siteId)
        {
            var siteName = RemoveInvalidChars(siteId);
            var results = new List<FavoriteDbCategory>();
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = string.Format("select * from Favorite_Categories where CAT_SITE_ID = '{0}'", siteName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new FavoriteDbCategory() {
                                Name = reader.GetString(reader.GetOrdinal("CAT_Name")),
                                Description = reader.GetString(reader.GetOrdinal("CAT_Desc")),
                                Thumb = reader.GetString(reader.GetOrdinal("CAT_ThumbUrl")),
                                Id = reader.GetInt32(reader.GetOrdinal("CAT_ID")),
                                RecursiveName = reader.GetString(reader.GetOrdinal("CAT_Hierarchy"))
                            });
                        }
                    }
                }
            }
            return results;
        }

        public List<string> GetFavoriteCategoriesNames(string siteId)
        {
            var siteName = RemoveInvalidChars(siteId);
            var results = new List<string>();
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = string.Format("select CAT_Hierarchy from Favorite_Categories where CAT_SITE_ID = '{0}'", siteName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(reader.GetString(reader.GetOrdinal("CAT_Hierarchy")));
                        }
                    }
                }
            }
            return results;
        }

        public bool AddFavoriteCategory(Category category, string siteId)
        {
            var siteName = RemoveInvalidChars(siteId);
            string categoryHierarchyName = RemoveInvalidChars(category.RecursiveName("|"));

            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    //check if the category is already in the favorite list
                    cmd.CommandText = string.Format("select CAT_ID from FAVORITE_Categories where CAT_Hierarchy='{0}' AND CAT_SITE_ID='{1}'", categoryHierarchyName, siteName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            Log.Info("Favorite Category {0} already in database", category.Name);
                            return true;
                        }
                    }
                    
                    Log.Info("inserting favorite category on site {0} with name: {1}, desc: {2}, image: {3}",
                        siteName, category.Name, category.Description, category.Thumb, siteName);

                    string sql = string.Format(
                            "insert into FAVORITE_Categories(CAT_Name,CAT_Desc,CAT_ThumbUrl,CAT_Hierarchy,CAT_SITE_ID)VALUES('{0}','{1}','{2}','{3}','{4}')",
                            RemoveInvalidChars(category.Name), category.Description == null ? "" : RemoveInvalidChars(category.Description), category.Thumb, categoryHierarchyName, siteName);
                    cmd.CommandText = sql;
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        Log.Info("Favorite Category {0} inserted successfully into database", category.Name);
                        return true;
                    }
                    else
                    {
                        Log.Warn("Favorite Category {0} failed to insert into database", category.Name);
                        return false;
                    }
                }
            }
        }

        public bool RemoveFavoriteCategory(FavoriteDbCategory category)
        {
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    string sql = string.Format("delete from Favorite_Categories where CAT_ID = '{0}'", category.Id);
                    cmd.CommandText = sql;
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool RemoveFavoriteCategory(string siteName, string recursiveCategoryName)
        {
            siteName = RemoveInvalidChars(siteName);
            using (var cnn = OpenConnection())
            {
                using (var cmd = cnn.CreateCommand())
                {
                    string sql = string.Format("delete from Favorite_Categories where CAT_Hierarchy='{0}' AND CAT_SITE_ID='{1}'", recursiveCategoryName, siteName);
                    cmd.CommandText = sql;
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
