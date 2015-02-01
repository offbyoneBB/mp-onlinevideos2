using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Linq;

namespace OnlineVideos.Sites
{
  public class DRTVUtil : SiteUtilBase
  {
    private string baseUrlDrNu = "http://www.dr.dk/mu-online/api/1.1";
    string bonanza_url = "http://www.dr.dk/Bonanza/index.htm";
    string bonanzaKategori_regEx = @"<p><a\shref=""(?<url>/Bonanza/kategori[^""]+)"">(?<title>[^<]+)</a></p>";
    string bonanzaSerie_regEx = @"<a\shref=""(?<url>[^""]+)""[^>]*>\s*
<img\ssrc=""(?<thumb>[^""]+)""[^>]*>\s*
<b>(?<title>[^<]+)</b>\s*
<span>(?<description>[^<]+)</span>\s*
</a>(?=(?:(?!Redaktionens\sfavoritter).)*Redaktionens\sfavoritter)";
    string bonanzaVideolist_regEx = @"<a\starget=""_parent""(?:(?!onclick="").)*onclick=""bonanzaFunctions.newPlaylist\((?<url>[^""]+)\);""[^>]*>\s*
<img\ssrc=""(?<thumb>[^""]+)""[^>]*>\s*
<b>(?<title>[^<]+)</b>\s*
(?:(?!<div\sclass=""duration"">).)*<div\sclass=""duration"">(?<length>[^<]+)</div>\s*</a>";
    Regex regEx_bonanzaKategori, regEx_bonanzaSerie, regEx_bonanzaVideolist;


    public override void Initialize(SiteSettings siteSettings)
    {
      base.Initialize(siteSettings);
      if (!string.IsNullOrEmpty(bonanzaKategori_regEx)) regEx_bonanzaKategori = new Regex(bonanzaKategori_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
      if (!string.IsNullOrEmpty(bonanzaSerie_regEx)) regEx_bonanzaSerie = new Regex(bonanzaSerie_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
      if (!string.IsNullOrEmpty(bonanzaVideolist_regEx)) regEx_bonanzaVideolist = new Regex(bonanzaVideolist_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
    }


    public override string GetVideoUrl(VideoInfo video)
    {
      if (video.VideoUrl.Contains("+"))
      {
        string aUrl = "";
        video.PlaybackOptions = new Dictionary<string, string>();
        string[] qualities = video.VideoUrl.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string quality in qualities)
        {
          string[] q_l = quality.Split('+');
          if (aUrl == "") aUrl = q_l[1];
          q_l[1] = q_l[1].Replace("rtmp://", "");
          string filetype = q_l[1].Substring(q_l[1].Length - 3, 3);
          q_l[1] = q_l[1].Replace("bonanza/bonanza", "bonanza/" + filetype + ":bonanza");
          string[] paths = q_l[1].Split(':');
          string playpath = string.Empty;
          if (paths[0].Substring(paths[0].Length - 3, 3) == "mp4")
          {
            playpath = "mp4:" + paths[1].Substring(0, paths[1].Length - 4);
          }
          else
          {
            playpath = "flv:" + paths[1].Substring(0, paths[1].Length - 4);
          }
          string vUrl = new MPUrlSourceFilter.RtmpUrl("rtmp://" + q_l[1]) { PlayPath = playpath }.ToString();
          video.PlaybackOptions.Add(q_l[0], vUrl);
        }
        return aUrl;
      }
      else if (video.Other == "drlive")
      {
        string link = loadLiveAsset(video.VideoUrl);
        return link;
      }
      else if (video.Other == "drnu")
      {
        string link = loadAsset(video.VideoUrl);
        return link;
      }
      else
      {
        return base.GetVideoUrl(video);
      }
    }


    //Discover dynamic DR live-channels
    private List<VideoInfo> getlivestreams()
    {
      List<VideoInfo> res = new List<VideoInfo>();

      string webDataUrl = baseUrlDrNu + "/channel/all-active-dr-tv-channels/";
      string strchannels = GetWebData(webDataUrl);
      JArray arrchannels = JArray.Parse(strchannels);
      string[] parts = null;

      foreach (JObject channel in arrchannels)
      {
        try
        {
          if (!(bool)channel["WebChannel"])
          {
            VideoInfo video = new VideoInfo();
            video.Title = (string)channel["Title"];
            video.ImageUrl = (string)channel["PrimaryImageUri"];
            video.Other = "drlive";
            Log.Debug("DR NU Title: " + video.Title);
            JArray streamingservers = (JArray)channel["StreamingServers"];
            foreach (JObject srv in streamingservers)
            {
              if ((string)srv["LinkType"] == "HLS")
              {
                Log.Debug("DR NU HLS Target found");
                string server = (string)srv["Server"];
                string url = (string)srv["Qualities"][0]["Streams"][0]["Stream"];
                Log.Debug("DR NU link: " + server + "/" + url);
                video.VideoUrl = server + "/" + url;
                res.Add(video);
              }
            }
          }
        }
        catch
        {
        }
      }
      res = res.OrderBy(o => o.Title.Replace(" ", "")).ToList();
      return res;
    }

    //loadLiveAsset is called from getVideos and fetches Live TV m3u8 playlist for HLS media
    public string loadLiveAsset(string url)
    {
      string assetLink = null;
      string m3u8 = GetWebData(url);
      Log.Debug("DR NU m3u8: " + m3u8);
      int curr_bandwidth = 0;
      int new_bandwidth = 0;
      bool selectnext = false;
      string[] parts = null;
      string[] lines = m3u8.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
      foreach (string line in lines)
      {
        Log.Debug("DR NU m3u8 line: " + line);
        if (line.StartsWith("#EXT-"))
        {
          parts = line.Split(',');
          foreach (string part in parts)
          {
            if (part.StartsWith("BANDWIDTH="))
            {
              Int32.TryParse(part.Substring(10), out new_bandwidth);
              if (new_bandwidth > curr_bandwidth)
              {
                curr_bandwidth = new_bandwidth;
                selectnext = true;
              }
              else
              {
                selectnext = false;
              }

            }
          }
        }
        if (line.StartsWith("http://") && selectnext == true)
        {
          assetLink = line;
        }
      }
      return assetLink;
    }

    //loadAsset is called from getVideos and fetches m3u8 playlist for HLS media
    public string loadAsset(string url, string target = "HLS")
    {
      string struri = GetWebData(url);
      Log.Debug("DR NU struri: " + struri);
      string assetLink = null;
      string m3u8Link = null;
      JObject objuri = JObject.Parse(struri);
      JArray links = (JArray)objuri["Links"];
      foreach (JObject link in links)
      {
        if ((string)link["Target"] == target)
        {
          Log.Debug("DR NU HLS Target found");
          m3u8Link = (string)link["Uri"];
          Log.Debug("DR NU Uri: " + m3u8Link);
          string m3u8 = GetWebData(m3u8Link);
          Log.Debug("DR NU m3u8: " + m3u8);
          string[] lines = m3u8.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
          foreach (string line in lines)
          {
            Log.Debug("DR NU m3u8 line: " + line);
            if (line.StartsWith("http://"))
            {
              assetLink = line;
              break;
            }
          }
        }
      }
      return assetLink;
    }


    private List<VideoInfo> getVideos(JObject contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        JArray slugs = (JArray)contentData["Items"];
        Log.Debug("DR NU slugs count: " + slugs.Count);
        foreach (JObject slug in slugs)
        {
          try
          {
            string itemslug = slug.Value<string>("Slug");
            string link = null;
            TimeSpan duration = new TimeSpan(0);
            string fduration = null;
            string img = null;
            string webDataUrl = baseUrlDrNu + "/programcard/" + itemslug;
            Log.Debug("DR NU webDataUrl: " + webDataUrl);
            string strprogramcard = GetWebData(webDataUrl);
            JObject objprogramcard = JObject.Parse(strprogramcard);
            string itemTitle = (string)objprogramcard["Title"];
            DateTime airDate = (DateTime)objprogramcard["PrimaryBroadcastStartTime"];
            img = (string)objprogramcard["PrimaryImageUri"];
            string itemDescription = (string)objprogramcard["Description"];
            Log.Debug("DR NU Description: " + itemDescription);
            JObject assets = (JObject)objprogramcard["PrimaryAsset"];

            if (assets.Count > 0)
            {
              Log.Debug("DR NU asset count: " + assets.Count);
              string kind = (string)assets["Kind"];
              string uri = (string)assets["Uri"];

              if (kind == "VideoResource")
              {
                link = uri;
                duration = TimeSpan.FromMilliseconds((double)assets["DurationInMilliseconds"]);
                fduration = String.Format("{0:D2}:{1:D2}:{2:D2}", duration.Hours, duration.Minutes, duration.Seconds);
              }

              Log.Debug("DR NU Uri: " + uri);
            }
            if (link.Length > 0)
            {
              VideoInfo video = new VideoInfo();
              video.Title = itemTitle;
              video.Description = itemDescription;
              video.VideoUrl = link;
              video.Length = fduration;
              video.ImageUrl = img;
              video.Other = "drnu";
              video.Airdate = airDate.ToString("dd. MMM. yyyy kl. HH:mm");
              res.Add(video);
            }
          }
          catch
          {
          }
        }
      }
      return res;
    }


    public override List<VideoInfo> GetVideos(Category category)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      string[] myString = category.Other.ToString().Split(',');
      if (myString[0] == "drlive")
      {
        return getlivestreams();
      }

      if (myString[0] == "search")
      {
        string url = baseUrlDrNu + "/search/tv/programcards-with-asset/title/" + myString[1] + "?limit=75";
        Log.Debug("DR NU url: " + url);
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getVideos(contentData);
      }

      if (myString[0] == "drnulist_card")
      {
        string url = baseUrlDrNu + "/list/" + myString[1] + "?limit=75";
        Log.Debug("DR NU url: " + url);
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getVideos(contentData);
      }

      if (myString[0] == "drnulastchance")
      {
        string url = baseUrlDrNu + "/list/view/LastChance?limit=10";
        Log.Debug("DR NU url: " + url);
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getVideos(contentData);
      }

      if (myString[0] == "drnumostviewed")
      {
        string url = baseUrlDrNu + "/list/view/mostviewed?limit=10";
        Log.Debug("DR NU url: " + url);
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getVideos(contentData);
      }

      if (myString[0] == "drnuspot")
      {
        string url = baseUrlDrNu + "/list/view/selectedlist?limit=10";
        Log.Debug("DR NU url: " + url);
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getVideos(contentData);
      }

      if (myString[0] == "bonanza")
      {
        string url = ((RssLink)category).Url;
        string data = GetWebData(url);
        if (data.Length > 0)
        {
          Match m = regEx_bonanzaVideolist.Match(data);
          while (m.Success)
          {
            VideoInfo videoInfo = new VideoInfo() { Title = m.Groups["title"].Value, Length = m.Groups["length"].Value, ImageUrl = m.Groups["thumb"].Value };

            var info = JObject.Parse(HttpUtility.HtmlDecode(m.Groups["url"].Value));
            if (info != null)
            {
              videoInfo.Description = info.Value<string>("Description");
              DateTime parsedDate;
              if (DateTime.TryParse(info.Value<string>("FirstPublished"), out parsedDate)) videoInfo.Airdate = parsedDate.ToString("g", OnlineVideoSettings.Instance.Locale);
              foreach (var file in info["Files"])
              {
                if (file.Value<string>("Type").StartsWith("Video"))
                {
                  videoInfo.VideoUrl += (videoInfo.VideoUrl.Length > 0 ? "|" : "") + file.Value<string>("Type").Substring(5) + "+" + file.Value<string>("Location");
                }
              }
              res.Add(videoInfo);
            }
            m = m.NextMatch();
          }
        }
      }
      return res;
    }

    public override int DiscoverDynamicCategories()
    {

      //Add category Live TV
      RssLink mainCategory = new RssLink()
      {
        Name = "Live TV",
        HasSubCategories = false,
        SubCategoriesDiscovered = false,
        Description = "Se de 6 danske TV-kanaler DR1, DR2, DR3, DR Ramasjang, DR K og DR Ultra.",
        Url = "http://www.dr.dk/live",
        Other = "drlive,",
        EstimatedVideoCount = 6
      };
      Settings.Categories.Add(mainCategory);

      //Add category DR NU
      mainCategory = new RssLink()
      {
        Name = "DR NU",
        HasSubCategories = true,
        SubCategoriesDiscovered = false,
        Description = "Video fra DR NU.",
        Url = "http://www.dr.dk/mu",
        Other = "drnu,"
      };
      Settings.Categories.Add(mainCategory);

      //Add category Bonanza
      mainCategory = new RssLink()
      {
        Name = "Bonanza",
        HasSubCategories = true,
        SubCategoriesDiscovered = false,
        Description = "Video fra DR Bonaza arkivet. Bonanza er blevet til i samarbejde med, og en del af DRs Kulturarvsprojekt.",
        Url = bonanza_url,
        Other = "bonanza,start"
      };
      Settings.Categories.Add(mainCategory);

      Settings.DynamicCategoriesDiscovered = true;
      return Settings.Categories.Count;
    }

    public override int DiscoverSubCategories(Category parentCategory)
    {
      parentCategory.SubCategories = new List<Category>();
      RssLink parentCat = parentCategory as RssLink;
      string[] myString = parentCategory.Other.ToString().Split(',');

      if (myString[0] == "drnulist_az")
      {
        //Add alphabetic A-Å subcategories
        string[] alpha = new string[25] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "VW", "XYZ", "ÆØÅ", "0-9" };
        foreach (string a in alpha)
        {
          RssLink subCategory = new RssLink()
          {
            Name = a,
            ParentCategory = parentCategory,
            HasSubCategories = true,
            SubCategoriesDiscovered = false,
            Other = "drnulist_alpha," + a
          };
          parentCategory.SubCategories.Add(subCategory);
        }
      }

      if (myString[0] == "drnulist_alpha")
      {
        //Get series with chosen letter
        if (myString[1].Length > 1)
        {
          myString[1] = myString[1][0] + ".." + myString[1][myString[1].Length - 1];
        }
        string url = baseUrlDrNu + "/search/tv/programcards-latest-episode-with-asset/series-title-starts-with/" + myString[1] + "?limit=50";
        Log.Debug("DR NU url: " + url);
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        if (contentData != null)
        {
          JArray slugs = (JArray)contentData["Items"];
          foreach (JObject slug in slugs)
          {
            try
            {
              string itemTitle = slug.Value<string>("SeriesTitle");
              JObject assets = (JObject)slug["PrimaryAsset"];
              string itemslug = slug.Value<string>("SeriesSlug");
              VideoInfo video = new VideoInfo();
              RssLink subCategory = new RssLink()
              {
                Name = itemTitle,
                ParentCategory = parentCategory,
                HasSubCategories = false,
                SubCategoriesDiscovered = false,
                Other = "drnulist_card," + itemslug
              };
              parentCategory.SubCategories.Add(subCategory);
            }
            catch
            {
            }
          }
        }
      }


      //DR NU categories
      if (myString[0] == "drnu")
      {
        //Add static category A-Å program series
        RssLink subCategory = new RssLink()
        {
          Name = "Programmer A-Å",
          ParentCategory = parentCategory,
          HasSubCategories = true,
          SubCategoriesDiscovered = false,
          Other = "drnulist_az,"
        };
        parentCategory.SubCategories.Add(subCategory);

        //Add static category last chance
        subCategory = new RssLink()
        {
          Name = "Sidste chance",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 10,
          Other = "drnulastchance,"
        };
        parentCategory.SubCategories.Add(subCategory);

        //Add static category most viewed
        subCategory = new RssLink()
        {
          Name = "Mest sete",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 10,
          Other = "drnumostviewed,"
        };
        parentCategory.SubCategories.Add(subCategory);

        //Add static category highlights
        subCategory = new RssLink()
        {
          Name = "Højdepunkter",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 10,
          Other = "drnuspot,"
        };
        parentCategory.SubCategories.Add(subCategory);

      }

      //DR Bonanza
      if (myString[0] == "bonanza")
      {
        string data = GetWebData(parentCat.Url);
        if (!string.IsNullOrEmpty(data))
        {
          Regex regEx = (regEx_bonanzaKategori as Regex);
          if (myString[1] != "start") regEx = regEx_bonanzaSerie;
          Match m = regEx.Match(data);
          while (m.Success)
          {
            RssLink cat = new RssLink()
            {
              Url = m.Groups["url"].Value,
              Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim()),
              Description = m.Groups["description"].Value,
              Thumb = m.Groups["thumb"].Value,
              ParentCategory = parentCategory,
              Other = "bonanza,",
              HasSubCategories = parentCategory.Name == "Bonanza"
            };

            cat.Url = HttpUtility.HtmlDecode(cat.Url);
            if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(parentCat.Url), cat.Url).AbsoluteUri;
            parentCategory.SubCategories.Add(cat);
            m = m.NextMatch();
          }
          parentCategory.SubCategoriesDiscovered = true;
        }
      }
      return parentCategory.SubCategories.Count;
    }


    #region Search
    public override bool CanSearch { get { return true; } }

    public override List<ISearchResultItem> Search(string query, string category = null)
    {
      Category search = new Category();
      search.Other = "search," + query;
      return GetVideos(search).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
    }

    #endregion

  }
}
