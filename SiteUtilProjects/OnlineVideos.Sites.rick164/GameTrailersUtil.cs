using System;
using System.Collections.Generic;
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

      // Populate list of video items to skip (invalid video urls etc..)
      // No longer required however if issues pop-popup we can get new entries her.

      //videosToSkip.Add("gt/live");
      //videosToSkip.Add("gt time");
      videosToSkip.Add("comic-con");
      videosToSkip.Add("e3 " + DateTime.Now.Year);
    }

    public override string GetVideoUrl(VideoInfo video)
    {
      var data = GetWebData(video.VideoUrl);
      var videoUrl = "";
      var baseDownloadUrl = "http://www.gametrailers.com/feeds/video_download/";
      if (data != null && data.Length > 0)
      {
        if (regEx_PlaylistUrl != null)
        {
          try
          {
            var m = regEx_PlaylistUrl.Match(data);
            while (m.Success)
            {
              var contentid = HttpUtility.HtmlDecode(m.Groups["contentid"].Value);
              var token = HttpUtility.HtmlDecode(m.Groups["token"].Value);
              var finalDownloadUrl = baseDownloadUrl + contentid + "/" + token;

              var dataJson = GetWebData(finalDownloadUrl);
              var o = JObject.Parse(dataJson);
              videoUrl = o["url"].ToString().Replace("\"", "");
              break;
            }
          }
          catch (Exception eVideoUrlRetrieval)
          {
            Log.Debug("Error while retrieving Video Url: " + eVideoUrlRetrieval);
          }
          return videoUrl;
        }
      }
      return null;
    }

    public override List<VideoInfo> GetVideos(Category category)
    {
      var url = ((RssLink) category).Url;
      //Log.Debug("CATEGORY: " + category.Name + " | URL: " +url);
      return GetVideoList(url);
    }

    public override List<VideoInfo> GetNextPageVideos()
    {
      //Log.Debug("NEXT PAGE URL: " + nextPageUrl);
      return GetVideoList(nextPageUrl);
    }

    protected List<VideoInfo> GetVideoList(string url)
    {
      var videoList = new List<VideoInfo>();
      var data = GetWebData(url);

      // Make sure Regexp doesn't get copied over after search so save it in a new Regexp.
      var regExVideoListGameName = new Regex("");

      Regex regExVideoListTmp;
      string strRegExVideoList;

      var searchBase = "http://www.gametrailers.com/feeds/search/";

      // Replace Regexp with search friendly regexp
      if (url.StartsWith(searchBase))
      {
        strRegExVideoList =
          @"<meta\sitemprop=""url""\scontent=""http://www\.gametrailers\.com/[^""]*/(?<tmpTitle>[^""]*)""/>\s*<meta\sitemprop=""name""\scontent=""(?<Title>[^""]*)""/>\s*<meta\sitemprop=""thumbnailUrl""\scontent=""(?<ImageUrl>[^""]*)""/>\s*<meta\sitemprop=""description""\scontent=""(?<Description>[^""]*)""/>\s*<meta\sitemprop=""uploadDate""\scontent=""(?<Airdate>[^""]*)""/>\s*<meta\sitemprop=""duration""\scontent=""(?<Duration>[^""]*)""/>\s*<a\shref=""(?<VideoUrl>[^""]*)""\sclass=""thumbnail"">";
        regExVideoListTmp = new Regex(strRegExVideoList);
      }

      // Hardcode regexp as we probably need to add workaround later (when GT changes their per-category layout once again)
      else
      {
        strRegExVideoList =
          @"<meta\sitemprop=""url""\scontent=""http://www\.gametrailers\.com/[^""]*/(?<tmpTitle>[^""]*)""/>\s*<meta\sitemprop=""name""\scontent=""(?<Title>[^""]*)""/>\s*<meta\sitemprop=""thumbnailUrl""\scontent=""(?<ImageUrl>[^""]*)""/>\s*<meta\sitemprop=""description""\scontent=""(?<Description>[^""]*)""/>\s*<meta\sitemprop=""uploadDate""\scontent=""(?<Airdate>[^""]*)""/>\s*<meta\sitemprop=""duration""\scontent=""(?<Duration>[^""]*)""/>\s*<a\shref=""(?<VideoUrl>[^""]*)""\sclass=""thumbnail"">";
        //strRegEx_VideoList = @"<meta\sitemprop=""url""\scontent=""http://www\.gametrailers\.com/[^""]*/(?<tmpTitle>[^""]*)""/>\s*<meta\sitemprop=""name""\scontent=""(?<Title>[^""]*)""/>\s*<meta\sitemprop=""thumbnailUrl""\scontent=""(?<ImageUrl>[^""]*)""/>\s*<meta\sitemprop=""description""\scontent=""(?<Description>[^""]*)""/>\s*<meta\sitemprop=""uploadDate""\scontent=""(?<Airdate>[^""]*)""/>\s*<meta\sitemprop=""duration""\scontent=""(?<Duration>[^""]*)""/>\s*<a\shref=""(?<VideoUrl>[^""]*)""\sclass=""thumbnail"">";
        regExVideoListTmp = new Regex(strRegExVideoList);

        // Full video title regexp
        var strRegExVideoListGameName = @"<h3><a\shref=""[^""]*"">(?<gameName>[^<]*)</a></h3>\s*<h4><a\shref=""[^""]*""\sclass=""override_promo_font_color"">(?<Title>[^<]*)</a></h4>";
        regExVideoListGameName = new Regex(strRegExVideoListGameName);
      }
      if (data.Length > 0)
      {
        try
        {
          //Log.Debug("USED URL: " + url);
          //Log.Debug("USED REGEX: " + strRegExVideoList);
          var m = regExVideoListTmp.Match(data);
          var m2 = regExVideoListGameName.Match(data);

          var counter = 0;
          var videoCount = 0;
          while (m.Success)
          {
            var videoInfo = CreateVideoInfo();
            videoInfo.Title = m.Groups["Title"].Value.Replace("&acirc;", "'");
            videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;

            // Try to retrieve full title (gameName + title) since these are listed differently per category or spread out, couldn't match those easily with one Regexp.
            if (!url.StartsWith(searchBase))
            {
              while (m2.Success)
              {
                if (counter == videoCount)
                {
                  if (!videoInfo.VideoUrl.StartsWith("http://") || Equals(videoInfo.VideoUrl, string.Empty) ||
                      videosToSkip.Contains(videoInfo.Title.ToLower()) ||
                      videosToSkip.Contains(m2.Groups["gameName"].Value.ToLower()))
                  {
                    // Skip invalid entries, mostly E3 without valid URL matched by regex
                  }
                  else
                  {
                    if (!string.IsNullOrEmpty(m2.Groups["gameName"].Value) ||
                        m2.Groups["gameName"].Value.ToLower().Contains("comic-con"))
                    {
                      videoInfo.Title = HttpUtility.HtmlDecode(m2.Groups["gameName"].Value) + " - " +
                                        HttpUtility.HtmlDecode(m2.Groups["Title"].Value.Replace("&acirc;", "'"));
                    }
                  }

                  counter++;
                }
                else
                {
                  break;
                }
              }
            }

            // Video url check
            if (!videoInfo.VideoUrl.StartsWith("http://") || videoInfo.VideoUrl == string.Empty ||
                videosToSkip.Contains(videoInfo.Title.ToLower()) ||
                videosToSkip.Contains(m2.Groups["gameName"].Value.ToLower())
              )
            {
              Log.Debug("No valid video url found or invalid video");
              Log.Debug("URL:" + videoInfo.VideoUrl);
              Log.Debug("TITLE:" + m2.Groups["gameName"].Value.ToLower());

              videoCount++;
              m2 = m2.NextMatch();
            }
            else
            {
              videoInfo.Thumb = m.Groups["ImageUrl"].Value;
              videoInfo.Airdate = m.Groups["Airdate"].Value;
              videoInfo.Length =
                StringUtils.PlainTextFromHtml(m.Groups["Duration"].Value)
                  .Replace("M", "M ")
                  .Replace("S", "S")
                  .Replace("PT0H", "")
                  .Replace("PT1H", "1H ")
                  .Replace("PT", "")
                  .Replace("T", "")
                  .Trim();

              Log.Debug("Desc: " + m.Groups["Description"].Value);

              // Encoding by GT is reported as UTF-8 but it's not in most cases, temporary fix added for "'" character
              videoInfo.Description = m.Groups["Description"].Value.Replace("&acirc;", "'");
              Log.Debug("Desc (enc): " + videoInfo.Description);
              Log.Debug("---------------");
              Log.Debug("Description: " + videoInfo.Description);
              Log.Debug("title: " + videoInfo.Title);
              Log.Debug("Video URL: " + videoInfo.VideoUrl);
              Log.Debug("Image: " + videoInfo.Thumb);

              videoCount++;
              videoList.Add(videoInfo);
              m = m.NextMatch();
              m2 = m2.NextMatch();
            }
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
            var mNext = regEx_NextPage.Match(data);
            if (mNext.Success)
            {
              //Log.Debug("PAGE URL: " + mNext.Groups["url"].Value);
              //Log.Debug("VIDEO URL: "+ url);
              nextPageAvailable = true;
              nextPageUrl = mNext.Groups["url"].Value;
              if (!string.IsNullOrEmpty(nextPageRegExUrlFormatString))
                nextPageUrl = string.Format(nextPageRegExUrlFormatString, nextPageUrl);
              nextPageUrl = ApplyUrlDecoding(nextPageUrl, nextPageRegExUrlDecoding);
              nextPageUrl = url + nextPageUrl.Replace("?currentPage=", "&currentPage=");
            }
            else
            {
              var page = HttpUtility.ParseQueryString(new Uri(url).Query)["currentPage"];
              nextPageAvailable = true;
              nextPageUrl = url.Replace("currentPage=" + page, "currentPage=" + (int.Parse(page) + 1));
            }
            //Log.Debug("NEXTPAGE URL: " + nextPageUrl);
          }
          catch (Exception eNextPageRetrieval)
          {
            Log.Debug("Error while retrieving Next Page Url: " + eNextPageRetrieval);
          }
        }
      }
      return videoList;
    }

    public override int DiscoverSubCategories(Category parentCategory)
    {
      var url = ((RssLink) parentCategory).Url;
      parentCategory.SubCategories = new List<Category>();

      //Log.Debug("PARENT CATEGORY: " + url);
      var data = GetWebData(url);

      if (regEx_dynamicSubCategories != null)
      {
        var catNames = new List<string>
        {
          "Newest Media",
          "Must See Videos",
          "Review",
          "Preview",
          "Trailer",
          "Gameplay",
          "Features",
          "Interview",
          "GT Originals"
        };

        // Add static categories, no need to fetch them since Gametrailers will most likely keep them as is for a while.


        var m = regEx_dynamicSubCategories.Match(data);
        var counter = 0;

        while (m.Success && counter == 0)
        {
          var tmpUrl = m.Groups["url"].Value;

          foreach (var catName in catNames)
          {
            var cat = new RssLink();
            if (catName == "Newest Media")
            {
              cat.Url = tmpUrl + "/?sortBy=most_recent";
            }
            else
            {
              cat.Url = tmpUrl + "/?sortBy=most_recent&category=" + catName;
            }

            cat.Name = catName;
            cat.ParentCategory = parentCategory;
            parentCategory.SubCategories.Add(cat);

            //Log.Debug("CAT NAME: " + cat.Name + " CAT URL: " + cat.Url);
            counter++;
          }
        }
      }

      parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
      return parentCategory.SubCategories.Count;
    }

    public override List<SearchResultItem> Search(string query, string category = null)
    {
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

      return GetVideoList(searchUrl).ConvertAll(v => v as SearchResultItem);
    }
  }
}