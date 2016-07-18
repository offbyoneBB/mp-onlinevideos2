using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using OnlineVideos.MediaPortal2.ResourceAccess;

namespace Amazon.Importer
{
  public class LibraryImporter
  {
    // List of required databases for our import. If they don't exist locally, they will be downloaded first.
    readonly Dictionary<string, string> _databases = new Dictionary<string, string>
      {
        { "movies.db", "https://github.com/Sandmann79/xbmc/raw/master/script.module.amazon.database/lib/movies.db" },
        { "tv.db", "https://github.com/Sandmann79/xbmc/raw/master/script.module.amazon.database/lib/tv.db" },
      };

    private readonly Guid SHARE_ID_MOVIES = new Guid("{E7CAEABE-79F5-46D7-91CF-5CC5A0FBFC13}");
    private readonly Guid SHARE_ID_SERIES = new Guid("{4D7C1270-64BE-43F0-A8DC-F3643A8FCBED}");
    private readonly ResourcePath ROOT_PATH_MOVIES = RawTokenResourceProvider.ToProviderResourcePath("M");
    private readonly ResourcePath ROOT_PATH_SERIES = RawTokenResourceProvider.ToProviderResourcePath("T");

    public void ImportSqlite()
    {
      try
      {
        var dt = ReadMovies();
        ImportMoviesToMediaLibrary(dt);

        dt = ReadSeries();
        ImportSeriesToMediaLibrary(dt);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error: ", ex);
      }
    }

    private void ImportMoviesToMediaLibrary(DataTable dt)
    {
      IMediaLibrary ml = ServiceRegistration.Get<IMediaLibrary>();
      ISystemResolver sr = ServiceRegistration.Get<ISystemResolver>();
      string systemId = sr.LocalSystemId;
      bool shareCreated;
      var parentDirectory = GetOrCreateMediaSourceDirectory(ml, systemId, SHARE_ID_MOVIES, ROOT_PATH_MOVIES, "Amazon Prime Movies", new List<string> { "Video", "Movie" }, out shareCreated);

      // For now we only import once after share is created
      if (!shareCreated)
        return;

      Dictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();

      foreach (DataRow row in dt.Rows)
      {
        aspects.Clear();
        MediaItemAspect mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
        MediaItemAspect videoAspect = new MediaItemAspect(VideoAspect.Metadata);
        MediaItemAspect movieAspect = new MediaItemAspect(MovieAspect.Metadata);
        MediaItemAspect onlineVideosAspect = new MediaItemAspect(OnlineVideosAspect.Metadata);
        aspects.Add(MediaAspect.ASPECT_ID, mediaAspect);
        aspects.Add(VideoAspect.ASPECT_ID, videoAspect);
        aspects.Add(MovieAspect.ASPECT_ID, movieAspect);
        aspects.Add(OnlineVideosAspect.ASPECT_ID, onlineVideosAspect);

        string asin = (row["asin"] as string ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (asin == null)
          continue;

        ResourcePath path = RawTokenResourceProvider.ToProviderResourcePath("M/" + asin);

        mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/onlinebrowser");
        mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, row["movietitle"] as string);


        videoAspect.SetAttribute(VideoAspect.ATTR_STORYPLOT, row["plot"] as string);
        int value;
        if (TryCast(row, "audio", out value))
          videoAspect.SetAttribute(VideoAspect.ATTR_AUDIOSTREAMCOUNT, value);

        MovieInfo movie = new MovieInfo();
        movie.MovieName = row["movietitle"] as string;
        movie.Certification = row["mpaa"] as string;

        ICollection<string> values;
        if (TrySplit(row, "director", out values))
          movie.Directors.AddRange(values);

        if (TrySplit(row, "writer", out values))
          movie.Writers.AddRange(values);

        if (TrySplit(row, "genres", out values, '/'))
          movie.Genres.AddRange(values);

        if (TryCast(row, "year", out value))
          movie.Year = value;

        if (TryCast(row, "stars", out value))
          movie.TotalRating = value;

        if (TryCast(row, "votes", out value))
          movie.RatingCount = value;

        if (TryCast(row, "runtime", out value))
          movie.Runtime = value;

        if (TrySplit(row, "actors", out values))
          movie.Actors.AddRange(values);

        movie.SetMetadata(aspects);

        // TODO: support other siteutils
        onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_LONGURL, path.Serialize());
        onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_SITEUTIL, "Amazon Prime De");
        string uri;
        if (TryParseUrl(row, "fanart", out uri))
          onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_FANART, uri);
        if (TryParseUrl(row, "poster", out uri))
          onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_POSTER, uri);

        ml.AddOrUpdateMediaItem(parentDirectory, systemId, path, aspects.Values);
      }
    }

    private void ImportSeriesToMediaLibrary(DataTable dt)
    {
      IMediaLibrary ml = ServiceRegistration.Get<IMediaLibrary>();
      ISystemResolver sr = ServiceRegistration.Get<ISystemResolver>();
      string systemId = sr.LocalSystemId;
      bool shareCreated;
      var parentDirectory = GetOrCreateMediaSourceDirectory(ml, systemId, SHARE_ID_SERIES, ROOT_PATH_SERIES, "Amazon Prime Series", new List<string> { "Video", "Series" }, out shareCreated);

      // For now we only import once after share is created
      if (!shareCreated)
        return;

      Dictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();

      foreach (DataRow row in dt.Rows)
      {
        aspects.Clear();
        MediaItemAspect mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
        MediaItemAspect videoAspect = new MediaItemAspect(VideoAspect.Metadata);
        MediaItemAspect seriesAspect = new MediaItemAspect(SeriesAspect.Metadata);
        MediaItemAspect onlineVideosAspect = new MediaItemAspect(OnlineVideosAspect.Metadata);
        aspects.Add(MediaAspect.ASPECT_ID, mediaAspect);
        aspects.Add(VideoAspect.ASPECT_ID, videoAspect);
        aspects.Add(SeriesAspect.ASPECT_ID, seriesAspect);
        aspects.Add(OnlineVideosAspect.ASPECT_ID, onlineVideosAspect);

        string asin = (row["asin"] as string ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (asin == null)
          continue;

        ResourcePath path = RawTokenResourceProvider.ToProviderResourcePath("T/" + asin);

        mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/onlinebrowser");

        SeriesInfo seriesInfo = new SeriesInfo();
        seriesInfo.Series = row["seriestitle"] as string;
        seriesInfo.Episode = row["episodetitle"] as string;
        seriesInfo.Summary = row["plot"] as string;

        ICollection<string> values;
        if (TrySplit(row, "actors", out values))
          CollectionUtils.AddAll(seriesInfo.Actors, values);

        if (TrySplit(row, "genres", out values, '/'))
          CollectionUtils.AddAll(seriesInfo.Genres, values);

        string airDateVal = row["airdate"] as string;
        DateTime dtValue;
        if (DateTime.TryParse(airDateVal, out dtValue))
        {
          seriesInfo.FirstAired = dtValue;
        }

        int value;

        if (TryCast(row, "season", out value))
          seriesInfo.SeasonNumber = value;

        if (TryCast(row, "episode", out value))
          seriesInfo.EpisodeNumbers.Add(value);

        if (TryCast(row, "stars", out value))
          seriesInfo.TotalRating = value;

        if (TryCast(row, "votes", out value))
          seriesInfo.RatingCount = value;

        seriesInfo.SetMetadata(aspects);

        // TODO: support other siteutils
        onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_SITEUTIL, "Amazon Prime De");
        string uri;
        if (TryParseUrl(row, "fanart", out uri))
          onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_FANART, uri);
        if (TryParseUrl(row, "poster", out uri))
          onlineVideosAspect.SetAttribute(OnlineVideosAspect.ATTR_POSTER, uri);

        ml.AddOrUpdateMediaItem(parentDirectory, systemId, path, aspects.Values);
      }
    }

    private Guid GetOrCreateMediaSourceDirectory(IMediaLibrary ml, string systemId, Guid shareId, ResourcePath rootPath, string mediaSourceName, IEnumerable<string> categories, out bool shareCreated)
    {
      Guid parentDirectory = Guid.Empty;
      shareCreated = false;

      var share = ml.GetShare(shareId);
      if (share == null)
      {
        share = new Share(shareId, systemId, rootPath, mediaSourceName, categories);
        ml.RegisterShare(share);
        shareCreated = true;
      }

      MediaItemAspect directoryAspect = new MediaItemAspect(DirectoryAspect.Metadata);
      parentDirectory = ml.AddOrUpdateMediaItem(parentDirectory, systemId, rootPath, new[] { directoryAspect });
      return parentDirectory;
    }

    protected bool TryCast<TE>(DataRow row, string colName, out TE value)
    {
      if (row[colName] == DBNull.Value)
      {
        value = default(TE);
        return false;
      }
      value = (TE)Convert.ChangeType(row[colName], typeof(TE));
      return value != null;
    }

    protected bool TryParseUrl(DataRow row, string colName, out string uri)
    {
      Uri result;
      if (Uri.TryCreate(row[colName] as string, UriKind.Absolute, out result))
      {
        uri = result.ToString();
        return true;
      }
      uri = null;
      return false;
    }

    protected bool TrySplit(DataRow row, string colName, out ICollection<string> values, char delimiter = ',')
    {
      values = (row[colName] as string ?? string.Empty).Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries).Select(val => val.Trim()).ToList();
      return values.Count > 0;
    }

    private DataTable ReadMovies()
    {
      string localPath = GetLocalDatabase("movies.db");
      var connBuilder = new SQLiteConnectionStringBuilder { FullUri = localPath };

      using (SQLiteConnection connection = new SQLiteConnection(connBuilder.ToString()))
      {
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
          cmd.CommandText = "select * from movies where isPrime = 1 order by upper(movietitle)";
          DataTable dt = new DataTable();
          using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
            da.Fill(dt);

          connection.Close();
          return dt;
        }
      }
    }

    private DataTable ReadSeries()
    {
      string localPath = GetLocalDatabase("tv.db");
      var connBuilder = new SQLiteConnectionStringBuilder { FullUri = localPath };

      using (SQLiteConnection connection = new SQLiteConnection(connBuilder.ToString()))
      {
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
          cmd.CommandText = "select * from episodes where isPrime = 1 order by upper(seriestitle), season, episode";
          DataTable dt = new DataTable();
          using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
            da.Fill(dt);

          connection.Close();
          return dt;
        }
      }
    }

    private string GetLocalDatabase(string db)
    {
      var dbPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OnlineVideos\DB");
      if (!Directory.Exists(dbPath))
        Directory.CreateDirectory(dbPath);

      var database = _databases[db];
      var path = Path.Combine(dbPath, db);
      if (!File.Exists(path))
      {
        ServiceRegistration.Get<ILogger>().Info("AP Library Importer: Database {0} doesn't exist locally yet. Starting download...", db);
        using (WebClient client = new WebClient())
        {
          client.DownloadFile(database, path);
          ServiceRegistration.Get<ILogger>().Info("AP Library Importer: Database {0} download successful...", db);
        }
      }
      return path;
    }
  }
}
