using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.Database;
using SQLite.NET;
using System.Xml;
using System.IO;
using MediaPortal.Util;
using MediaPortal.Configuration;

namespace OnlineVideos.Database
{
  public class FavoritesDatabase
  {   
    private SQLiteClient m_db;

    private static FavoritesDatabase Instance;

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
          CreateTables();
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

    public static FavoritesDatabase getInstance()
    {
      if (Instance == null)
      {
        Instance = new FavoritesDatabase();
      }
      return Instance;
    }
    private void CreateTables()
    {
      if (m_db == null)
      {
        return;
      }
      try
      {
        m_db.Execute("CREATE TABLE FAVORITE_VIDEOS(VDO_ID integer primary key autoincrement,VDO_NM text,VDO_URL text,VDO_DESC text,VDO_TAGS text,VDO_LENGTH text,VDO_OTHER_NFO text,VDO_IMG_URL text,VDO_SITE_ID text)\n");
        //m_db.Execute("CREATE TABLE FAVORITE(FAVORITE_ID integer primary key,FAVORITE_NM text)\n");
        
        
        
        //if (loFavoriteVideos.Count > 0)
        //{
          //foreach (YahooVideo loVideo in loFavoriteVideos)
          //{
          //  addFavoriteVideo("Default", loVideo);
          //}
        //}
      }
      catch (Exception e)
      {
        Log.Info(e.ToString());
      }
    }
    public bool addFavoriteVideo(OnlineVideos.VideoInfo foVideo, string siteName)
    {

        //check if the video is already in the favorite list
        //lsSQL = string.Format("select SONG_ID from FAVORITE_VIDEOS where SONG_ID='{0}' AND COUNTRY='{1}' and FAVORITE_ID=''", foVideo.songId, foVideo.countryId, lsFavID);
        //loResultSet = m_db.Execute(lsSQL);
        //if (loResultSet.Rows.Count > 0)
        //{
        //    return false;
        //}
        Log.Info("inserting favorite:");
        Log.Info("desc:" + foVideo.Description);
        Log.Info("image:" + foVideo.ImageUrl);
        Log.Info("tags:" + foVideo.Tags);
        Log.Info("title:" + foVideo.Title);
        Log.Info("url" + foVideo.VideoUrl);

        foVideo.Description = DatabaseUtility.RemoveInvalidChars(foVideo.Description);
        foVideo.ImageUrl = DatabaseUtility.RemoveInvalidChars(foVideo.ImageUrl);
        foVideo.Tags = DatabaseUtility.RemoveInvalidChars(foVideo.Tags);
        foVideo.Title = DatabaseUtility.RemoveInvalidChars(foVideo.Title);
        foVideo.VideoUrl = DatabaseUtility.RemoveInvalidChars(foVideo.VideoUrl);

        string lsSQL =
            string.Format(
                "insert into FAVORITE_VIDEOS(VDO_NM,VDO_URL,VDO_DESC,VDO_TAGS,VDO_LENGTH,VDO_IMG_URL,VDO_SITE_ID)VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                foVideo.Title, foVideo.VideoUrl, foVideo.Description, foVideo.Tags, foVideo.Length, foVideo.ImageUrl,
                siteName);
        m_db.Execute(lsSQL);
        if (m_db.ChangedRows() > 0)
        {
            Log.Info("Favorite {0} inserted successfully into database", foVideo.Title);
            return true;
        }
        else
        {
            Log.Info("Favorite {0} failed to insert into database", foVideo.Title);
            return false;
        }
    }


      public bool removeFavoriteVideo(VideoInfo foVideo)
    {    	     
      String lsSQL = string.Format("delete from FAVORITE_VIDEOS where VDO_ID='{0}' ", foVideo.Other.ToString());
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
     

    public List<VideoInfo> getAllFavoriteVideos()
    {
    	return getFavoriteVideos(false,null);
    }
    public List<VideoInfo> getSiteFavoriteVideos(String fsSiteId){
    	return getFavoriteVideos(true,fsSiteId);
    }
    private List<VideoInfo> getFavoriteVideos(bool fbLimitBySite, String fsSiteId){
    	
      //createFavorite("Default2");
      string lsSQL;
      if(!fbLimitBySite){
      	lsSQL = string.Format("select * from favorite_videos");
      }else{
      	lsSQL = string.Format("select * from favorite_videos where VDO_SITE_ID='{0}'",fsSiteId);
      }
      SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
      List<VideoInfo> loFavoriteList = new List<VideoInfo>();
      if (loResultSet.Rows.Count == 0) return loFavoriteList ;
      
        for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
        {
            string sitename = DatabaseUtility.Get(loResultSet,iRow,"VDO_SITE_ID");
            if (OnlineVideoSettings.getInstance().SiteList.ContainsKey(sitename))
            {
                SiteSettings aSite = OnlineVideoSettings.getInstance().SiteList[sitename].Settings;

                if (aSite.IsEnabled &&
                   (!aSite.ConfirmAge || !OnlineVideoSettings.getInstance().useAgeConfirmation || OnlineVideoSettings.getInstance().ageHasBeenConfirmed))
                {
                    VideoInfo video = new VideoInfo();
                    video.Description = DatabaseUtility.Get(loResultSet, iRow, "VDO_DESC");
                    video.ImageUrl = DatabaseUtility.Get(loResultSet, iRow, "VDO_IMG_URL");
                    video.Length = DatabaseUtility.Get(loResultSet, iRow, "VDO_LENGTH");
                    video.Tags = DatabaseUtility.Get(loResultSet, iRow, "VDO_TAGS");
                    video.Title = DatabaseUtility.Get(loResultSet, iRow, "VDO_NM");
                    video.VideoUrl = DatabaseUtility.Get(loResultSet, iRow, "VDO_URL");
                    video.SiteName = sitename;
                    video.Other = DatabaseUtility.GetAsInt(loResultSet, iRow, "VDO_ID");
                    Log.Info("Pulled {0} out of the database", video.Title);
                    loFavoriteList.Add(video);
                }
            }
        }
        return loFavoriteList;
    }

    public string [] getSiteIDs(){
    	string lsSQL = "select distinct VDO_SITE_ID from favorite_videos";
    	SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
    	string [] siteIdList = new string[loResultSet.Rows.Count];
    	 for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
        {
    	 	siteIdList[iRow] = DatabaseUtility.Get(loResultSet,iRow,"VDO_SITE_ID");
    	 		
    	 }
    	 return siteIdList;    	
    }
    
     public List<VideoInfo> searchFavoriteVideos(String fsQuery){
    	
      //createFavorite("Default2");
      string lsSQL;
      //if(!fbLimitBySite){
      	lsSQL = string.Format("select * from favorite_videos where VDO_NM like '%{0}%' or VDO_DESC like '%{0}%' or VDO_TAGS like '%{0}%'",fsQuery);
      //}else{
      	//lsSQL = string.Format("select * from favorite_videos where VDO_SITE_ID='{0}'",fsSiteId);
      //}
      SQLiteResultSet loResultSet = m_db.Execute(lsSQL);
      List<VideoInfo> loFavoriteList = new List<VideoInfo>();
      if (loResultSet.Rows.Count == 0) return loFavoriteList ;
      
        for (int iRow = 0; iRow < loResultSet.Rows.Count; iRow++)
        {
            VideoInfo video = new VideoInfo();
        	video.Description = DatabaseUtility.Get(loResultSet, iRow, "VDO_DESC");
        	video.ImageUrl = DatabaseUtility.Get(loResultSet, iRow, "VDO_IMG_URL");
        	video.Length = DatabaseUtility.Get(loResultSet, iRow, "VDO_LENGTH");
        	video.Tags = DatabaseUtility.Get(loResultSet, iRow, "VDO_TAGS");
        	video.Title = DatabaseUtility.Get(loResultSet, iRow, "VDO_NM");
        	video.VideoUrl = DatabaseUtility.Get(loResultSet,iRow,"VDO_URL");
			video.SiteName = DatabaseUtility.Get(loResultSet,iRow,"VDO_SITE_ID");
            video.Other = DatabaseUtility.GetAsInt(loResultSet, iRow, "VDO_ID");
        	Log.Info("Pulled {0} out of the database",video.Title);
        	loFavoriteList.Add(video);
        	
        }
        return loFavoriteList;
    }    
  }
}
