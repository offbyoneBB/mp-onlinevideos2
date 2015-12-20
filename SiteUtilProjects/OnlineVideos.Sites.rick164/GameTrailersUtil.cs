using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
  public class GameTrailersUtil : GenericSiteUtil
  {
    private readonly List<string> videosToSkip = new List<string>();

    public override void Initialize(SiteSettings siteSettings)
    {
      base.Initialize(siteSettings);
    }

    public override List<VideoInfo> GetVideos(Category category)
    {
      var url = ((RssLink) category).Url;
      nextPageUrl = string.Empty;
      return GetVideoList(url);
    }

    protected List<VideoInfo> GetVideoList(string url)
    {
      var videoList = new List<VideoInfo>();
      var data = GetWebData(url);

      if (data.Length > 0)
      {
        try
        {
          var m = regEx_VideoList.Match(data);

          while (m.Success)
          {
            var videoInfo = CreateVideoInfo();
            videoInfo.Title = string.Format("{0} - {1}", m.Groups["Title"].Value.Replace("&acirc;", "'"), m.Groups["Title2"].Value.Replace("&acirc;", "'"));
            videoInfo.VideoUrl = string.Format("http://www.gametrailers.com{0}", m.Groups["VideoUrl"].Value);
            videoInfo.Thumb = m.Groups["ImageUrl"].Value;
            videoInfo.Airdate = m.Groups["Airdate"].Value.Replace(":00+00:00", string.Empty).Replace("+00:00", string.Empty).Replace("T", " ");
            videoInfo.Description = m.Groups["Description"].Value.Replace("&acirc;", "'");

            videoList.Add(videoInfo);
            m = m.NextMatch();
          }
        }
        catch (Exception eVideoListRetrieval)
        {
          Log.Debug("Error while retrieving VideoList: " + eVideoListRetrieval);
        }

        if (regEx_NextPage != null)
        {
          try
          {
            // Check for next page link
            // Workaround as GT no longer reliably lists page navivation counts
            var mNext = regEx_NextPage.Match(data);
            if (mNext.Success)
            {
              if (string.IsNullOrEmpty(nextPageUrl))
              {
                nextPageAvailable = true;

                var uriBuilder = new UriBuilder(url);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["page"] = "1";
                uriBuilder.Query = query.ToString();
                nextPageUrl = ApplyUrlDecoding(uriBuilder.ToString(), nextPageRegExUrlDecoding);
              }
              else
              {
                Uri myUri = new Uri(nextPageUrl);
                string strPageCount = HttpUtility.ParseQueryString(myUri.Query).Get("page");
                Int32 pageCount;
                bool isInteger = Int32.TryParse(strPageCount, out pageCount);

                if (isInteger)
                {
                  nextPageAvailable = true;
                  pageCount++;
                  var uriBuilder = new UriBuilder(url);
                  var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                  query["page"] = pageCount.ToString();
                  uriBuilder.Query = query.ToString();
                  nextPageUrl = ApplyUrlDecoding(uriBuilder.ToString(), nextPageRegExUrlDecoding);
                }
              }

              Log.Debug("PAGE URL: " + nextPageUrl);
              Log.Debug("VIDEO URL: " + url);
            }
          }
          catch (Exception eNextPageRetrieval)
          {
            Log.Debug("Error while retrieving Next Page Url: " + eNextPageRetrieval);
          }
        }
      }
      return videoList;
    }

    public override List<VideoInfo> GetNextPageVideos()
    {
      return GetVideoList(nextPageUrl);
    }

    public override int DiscoverSubCategories(Category parentCategory)
    {
      var url = ((RssLink) parentCategory).Url;
      parentCategory.SubCategories = new List<Category>();

      var catNames = new List<string>
      {
        "Newest Media",
        "Review",
        "Preview",
        "Trailer",
        "Gameplay",
        "Features",
        "Interviews"
      };

      foreach (var catName in catNames)
      {
        var cat = new RssLink();
        if (catName == "Newest Media")
        {
          cat.Url = string.Format("{0}", url);
        }
        else
        {
          cat.Url = string.Format("{0}?tags={1}", url, catName);
        }

        cat.Name = catName;
        cat.ParentCategory = parentCategory;
        parentCategory.SubCategories.Add(cat);
      }

      parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
      return parentCategory.SubCategories.Count;
    }

    public override string GetVideoUrl(VideoInfo video)
    {
      var itemPlaylist = Regex.Match(GetWebData(video.VideoUrl), playlistUrlRegEx);
      var playlistUrl = "";
      while (itemPlaylist.Success)
      {
        playlistUrl = string.Format("http://embed.gametrailers.com/embed/{0}", itemPlaylist.Groups["url"].Value);
        break;
      }
      Log.Debug("GT - Playlist url: " + playlistUrl);

      Log.Debug(fileUrlRegEx);
      var itemfileUrl = Regex.Match(GetWebData(playlistUrl), fileUrlRegEx);
      var fileUrl = "";
      while (itemfileUrl.Success)
      {
        fileUrl = itemfileUrl.Groups["m0"].Value;
        break;
      }
      Log.Debug("GT - File url: " + fileUrl);

      return fileUrl;
    }


    public override List<SearchResultItem> Search(string query, string category = null)
    {
      /*
      // First we need to fetch the Promo ID in order to do a real search
      var strPromoIDRegex =
        @"<input class=""search"" name=""keywords"" type=""text"" value=""[^""]*"" data-keywords=""[^""]*"" data-promotionId=(?<id>[^/]*)/>";
      var regExSearchUrl = new Regex(strPromoIDRegex);

      var data = GetWebData("http://www.gametrailers.com/search?keywords=");
      var promotionId = "";
      var m = regExSearchUrl.Match(data);
      try
      {
        while (m.Success)
        {
          promotionId = m.Groups["id"].Value;
          //Log.Debug("PROMO=" + promotionID);
          break;
        }
      }
      catch (Exception eSearchUrlRetrieval)
      {
        Log.Debug("Error while retrieving Search URL: " + eSearchUrlRetrieval);
      }

      // If an override Encoding was specified, we need to UrlEncode the search string with that encoding
      if (encodingOverride != null) query = HttpUtility.UrlEncode(encodingOverride.GetBytes(query));

      searchUrl =
        string.Format(
          "http://www.gametrailers.com/feeds/search/child/{0}/?keywords={1}&tabName=videos&platforms=&sortBy=most_recent",
          promotionId, query);

      return GetVideoList(searchUrl).ConvertAll(v => v as SearchResultItem);*/
      return null;
    }
  }
}