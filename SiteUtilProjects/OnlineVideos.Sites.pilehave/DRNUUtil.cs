using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
  public class DRTVUtil : SiteUtilBase
  {

    private string baseUrlDrNu = "http://www.dr.dk/nu/api";
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

    public override string getUrl(VideoInfo video)
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
      else
      {
        return base.getUrl(video);
      }
    }

    //Add static DR channels
    private List<VideoInfo> getlivestreams()
    {
      List<VideoInfo> res = new List<VideoInfo>();
      string[] channels = new string[6] { "DR1", "DR2", "DR Ultra", "DR K", "DR Ramasjang", "DR 3" };
      string[] paths = new string[6] { "astream3", "astream3", "astream3", "astream3", "astream3", "bstream3" };
      for (int i = 0; i < 6; i++)
      {
        VideoInfo video = new VideoInfo();
        video.Title = channels[i];
        //New URL: rtmp://livetv.gss.dr.dk/live/livedr0
        //Old URL: rtmp://rtmplive.dr.dk/live/livedr0
        video.VideoUrl = new MPUrlSourceFilter.RtmpUrl("rtmp://livetv.gss.dr.dk/live/livedr0" + (i + 1) + paths[i]) { Live = true }.ToString();
        res.Add(video);
      }
      return res;
    }

    private List<VideoInfo> getSpecialvideos(JArray contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        foreach (var item in contentData)
        {
          string url = baseUrlDrNu + "/videos/" + item.Value<string>("id");
          string json = GetWebData(url);
          JObject subContentData = JObject.Parse(json);
          if (subContentData != null)
          {
            string redirectUrl = GetWebData(subContentData.Value<string>("videoManifestUrl"));
            VideoInfo video = new VideoInfo();
            video.Title = subContentData.Value<string>("title");
            video.Description = subContentData.Value<string>("description");

            //Nyt

            // ('(rtmp://vod.dr.dk/cms)/([^\?]+)(\?.*)', rtmpUrl)

            Match match = Regex.Match(redirectUrl, @"(rtmp://vod.dr.dk/cms)/([^\?]+)(\?.*)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
              Log.Info("videosti fundet");
              video.VideoUrl = match.Groups[1].Value + match.Groups[3].Value + " playpath=" + match.Groups[2].Value + match.Groups[3].Value + " app=cms" + match.Groups[3].Value;
              Log.Info("Her: " + video.VideoUrl);
            }
            //Nyt slut


            //video.VideoUrl = redirectUrl.Replace("rtmp://vod.dr.dk/", "rtmp://vod.dr.dk/cms/");
            video.ImageUrl = baseUrlDrNu + "/videos/" + item.Value<string>("id") + "/images/400x225.jpg";
            video.Length = subContentData.Value<string>("duration");
            video.Airdate = subContentData.Value<string>("formattedBroadcastTime");
            res.Add(video);
          }
        }
      }
      return res;
    }

    private List<VideoInfo> getvideos(JArray contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        foreach (var item in contentData)
        {
          string redirectUrl = GetWebData(item.Value<string>("videoManifestUrl"));
          VideoInfo video = new VideoInfo();
          video.Title = item.Value<string>("title");
          video.Description = item.Value<string>("description");
          video.VideoUrl = redirectUrl.Replace("rtmp://vod.dr.dk/", "rtmp://vod.dr.dk/cms/");
          video.ImageUrl = baseUrlDrNu + "/videos/" + item.Value<string>("id") + "/images/400x225.jpg";
          video.Length = item.Value<string>("duration");
          video.Airdate = item.Value<string>("formattedBroadcastTime");
          res.Add(video);
        }
      }
      return res;
    }

    public override List<VideoInfo> getVideoList(Category category)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      string[] myString = category.Other.ToString().Split(',');
      if (myString[0] == "drlive")
      {
        return getlivestreams();
      }

      if (myString[0] == "drnu")
      {
        string url = baseUrlDrNu + "/programseries/" + myString[1] + "/videos";
        string json = GetWebData(url);
        JArray contentData = JArray.Parse(json);
        return getvideos(contentData);
      }

      if (myString[0] == "drnunewest")
      {
        string url = baseUrlDrNu + "/videos/newest";
        string json = GetWebData(url);
        JArray contentData = JArray.Parse(json);
        return getSpecialvideos(contentData);
      }

      if (myString[0] == "drnuhighlight")
      {
        string url = baseUrlDrNu + "/videos/highlight";
        string json = GetWebData(url);
        JArray contentData = JArray.Parse(json);
        return getSpecialvideos(contentData);
      }

      if (myString[0] == "drnumostviewed")
      {
        string url = baseUrlDrNu + "/videos/mostviewed";
        string json = GetWebData(url);
        JArray contentData = JArray.Parse(json);
        return getvideos(contentData);
      }

      if (myString[0] == "drnulastchance")
      {
        string url = baseUrlDrNu + "/videos/lastchance";
        string json = GetWebData(url);
        JArray contentData = JArray.Parse(json);
        return getvideos(contentData);
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

            var info = Newtonsoft.Json.Linq.JObject.Parse(HttpUtility.HtmlDecode(m.Groups["url"].Value));
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
        Description = "Se de 6 danske TV-kanaler DR1, DR2, DR3, DR Ramasjang, DR Update og DR Ultra.",
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
        Url = "http://www.dr.dk/nu/api",
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

      //DR NU
      if (myString[0] == "drnu")
      {
        //Add static categories newest, spot and most viewed
        RssLink subCategory = new RssLink()
        {
          Name = "Nyeste",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          Other = "drnunewest,"
        };
        parentCategory.SubCategories.Add(subCategory);

        subCategory = new RssLink()
        {
          Name = "Højdepunkter",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 9,
          Other = "drnuhighlight,"
        };
        parentCategory.SubCategories.Add(subCategory);

        subCategory = new RssLink()
        {
          Name = "Mest sete",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 9,
          Other = "drnumostviewed,"
        };
        parentCategory.SubCategories.Add(subCategory);

        subCategory = new RssLink()
        {
          Name = "Sidste chance",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 9,
          Other = "drnulastchance,"
        };
        parentCategory.SubCategories.Add(subCategory);

        string json = GetWebData(parentCat.Url + "/programseries");
        if (!string.IsNullOrEmpty(json))
        {
          foreach (var item in JArray.Parse(json))
          {
            subCategory = new RssLink()
            {
              Name = item.Value<string>("title"),
              HasSubCategories = false,
              SubCategoriesDiscovered = false,
              ParentCategory = parentCat,
              Description = item.Value<string>("description"),
              EstimatedVideoCount = item.Value<uint>("videoCount"),
              Other = "drnu," + item.Value<string>("slug"),
              Thumb = baseUrlDrNu + "/programseries/" + item.Value<string>("slug") + "/images/400x225.jpg"
            };
            parentCategory.SubCategories.Add(subCategory);
          }
          parentCategory.SubCategoriesDiscovered = true;
        }
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
  }
}
