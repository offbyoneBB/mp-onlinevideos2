using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
  public class DRTVUtil : SiteUtilBase
  {

    private string baseUrlDrNu = "http://www.dr.dk/mu";
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

    //Add static DR live-channels
    private List<VideoInfo> getlivestreams()
    {
      List<VideoInfo> res = new List<VideoInfo>();
      string[] channels = new string[6] { "DR 1", "DR 2", "DR 3", "DR K", "DR Ramasjang", "DR Ultra" };
      string[] paths = new string[6] { "1astream3", "2astream3", "6astream3", "4astream3", "5astream3", "3astream3" };
      for (int i = 0; i < 6; i++)
      {
        VideoInfo video = new VideoInfo();
        video.Title = channels[i];
        video.VideoUrl = new MPUrlSourceFilter.RtmpUrl("rtmp://livetv.gss.dr.dk/live/livedr0" + paths[i]) { Live = true }.ToString();
        res.Add(video);
      }
      return res;
    }


    public string loadAsset(string url, string target = "Android")
    {
      string struri = GetWebData(url);
      string link = "";
      int bitrate = 0;
      JObject objuri = JObject.Parse(struri);
      JArray links = (JArray)objuri["Links"];
      for (int ilinks = 0; ilinks < links.Count; ilinks++)
      {
        if ((string)links[ilinks]["Target"] == target && (int)links[ilinks]["Bitrate"] > bitrate)
        {
          link = (string)links[ilinks]["Uri"];
          bitrate = (int)links[ilinks]["Bitrate"];
        }
      }
      link = link.Replace("rtsp://om.gss.dr.dk", "rtmp://vod-prio3.gss.dr.dk");
      return link;
    }



    private List<VideoInfo> getvideosSpot(JObject contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        JArray slugs = (JArray)contentData["Data"][0]["Relations"];

        foreach (var slug in slugs)
        {
          try
          {
            string itemslug = slug.Value<string>("Slug");
            string link = null;
            TimeSpan duration = new TimeSpan(0);
            string img = null;
            string webDataUrl = baseUrlDrNu + "/programcard/expanded/" + itemslug;
            string strprogramcard = GetWebData(webDataUrl);
            JObject objprogramcard = JObject.Parse(strprogramcard);
            string itemTitle = (string)objprogramcard["Data"][0]["Broadcasts"][0]["Title"];
            string itemDescription = (string)objprogramcard["Data"][0]["Broadcasts"][0]["Description"];


            JArray assets = (JArray)objprogramcard["Data"][0]["Assets"];
            if (assets.Count > 0)
            {
              for (int iasset = 0; iasset < assets.Count; iasset++)
              {
                string kind = (string)objprogramcard["Data"][0]["Assets"][iasset]["Kind"];
                string uri = (string)objprogramcard["Data"][0]["Assets"][iasset]["Uri"];
                if (kind == "VideoResource")
                {
                  string url = uri;
                  link = loadAsset(url);
                  duration = TimeSpan.FromMilliseconds((int)assets[iasset]["DurationInMilliseconds"]);
                }
                if (kind == "Image")
                {
                  img = uri;
                }
              }
            }
            VideoInfo video = new VideoInfo();
            video.Title = itemTitle;
            video.Description = itemDescription;
            video.VideoUrl = link;
            video.ImageUrl = img;
            video.Length = duration.ToString();
            res.Add(video);
          }
          catch
          {
          }
        }
      }
      return res;
    }


    private List<VideoInfo> getvideosSearch(JObject contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        JArray slugs = (JArray)contentData["Data"];
        for (int i = 0; i < slugs.Count; i++)
        {
          try
          {
            string link = null;
            TimeSpan duration = new TimeSpan(0);
            string img = null;
            string itemTitle = (string)slugs[i]["Title"];
            string itemDescription = (string)slugs[i]["Description"];
            JArray assets = (JArray)slugs[i]["Assets"];
            if (assets.Count > 0)
            {
              for (int iasset = 0; iasset < assets.Count; iasset++)
              {
                string kind = (string)assets[iasset]["Kind"];
                string uri = (string)assets[iasset]["Uri"];
                if (kind == "VideoResource")
                {
                  link = loadAsset(uri);
                  duration = TimeSpan.FromMilliseconds((int)assets[iasset]["DurationInMilliseconds"]);
                }
                if (kind == "Image")
                {
                  img = uri;
                }
              }
            }
            if (link.Length > 0)
            {
              VideoInfo video = new VideoInfo();
              video.Title = itemTitle;
              video.Description = itemDescription;
              video.VideoUrl = link;
              video.Length = duration.ToString();
              video.ImageUrl = img;
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


    private List<VideoInfo> getvideosAlpha(JObject contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        JArray slugs = (JArray)contentData["Data"];
        for (int i = 0; i < slugs.Count; i++)
        {
          try
          {
            JArray broadcasts = (JArray)contentData["Data"][i]["Broadcasts"];
            if (broadcasts.Count > 0)
            {
              string link = null;
              TimeSpan duration = new TimeSpan(0);
              string img = null;
              string itemTitle = (string)broadcasts[0]["Title"];
              string itemDescription = (string)broadcasts[0]["Description"];
              JArray assets = (JArray)contentData["Data"][i]["Assets"];
              if (assets.Count > 0)
              {
                for (int iasset = 0; iasset < assets.Count; iasset++)
                {
                  string kind = (string)assets[iasset]["Kind"];
                  string uri = (string)assets[iasset]["Uri"];
                  if (kind == "VideoResource")
                  {
                    link = loadAsset(uri);
                    duration = TimeSpan.FromMilliseconds((int)assets[iasset]["DurationInMilliseconds"]);
                  }
                  if (kind == "Image")
                  {
                    img = uri;
                  }
                }
              }
              if (link.Length > 0)
              {
                VideoInfo video = new VideoInfo();
                video.Title = itemTitle;
                video.Description = itemDescription;
                video.VideoUrl = link;
                video.Length = duration.ToString();
                video.ImageUrl = img;
                res.Add(video);
              }
            }
          }
          catch
          {
          }
        }
      }
      return res;
    }



    private List<VideoInfo> getvideosPremiere(JObject contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        JArray slugs = (JArray)contentData["Data"][0]["Relations"];
        for (int i = 0; i < slugs.Count; i++)
        {
          try
          {
            string itemslug = slugs[i].Value<string>("Slug");
            string link = null;
            TimeSpan duration = new TimeSpan(0);
            string img = null;
            string webDataUrl = baseUrlDrNu + "/programcard/expanded/" + itemslug;
            Log.Info("MPJ webDataUrl:" + webDataUrl);
            string strprogramcard = GetWebData(webDataUrl);
            JObject objprogramcard = JObject.Parse(strprogramcard);
            string itemTitle = (string)objprogramcard["Data"][0]["Broadcasts"][0]["Title"];
            string itemDescription = (string)objprogramcard["Data"][0]["Broadcasts"][0]["Description"];
            JArray assets = (JArray)objprogramcard["Data"][0]["Assets"];
            if (assets.Count > 0)
            {
              for (int iasset = 0; iasset < assets.Count; iasset++)
              {
                string kind = (string)assets[iasset]["Kind"];
                string uri = (string)assets[iasset]["Uri"];
                if (kind == "VideoResource")
                {
                  link = loadAsset(uri);
                  duration = TimeSpan.FromMilliseconds((int)assets[iasset]["DurationInMilliseconds"]);
                }

                if (kind == "Image")
                {
                  img = uri;
                }

              }
            }
            if (link.Length > 0)
            {
              VideoInfo video = new VideoInfo();
              video.Title = itemTitle;
              video.Description = itemDescription;
              video.VideoUrl = link;
              video.Length = duration.ToString();
              video.ImageUrl = img;
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



    private List<VideoInfo> getvideos(JObject contentData)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      if (contentData != null)
      {
        JArray slugs = (JArray)contentData["Data"];

        for (int i = 0; i < slugs.Count; i++)
        {
          try
          {
            string itemslug = slugs[i].Value<string>("Slug");
            string link = null;
            TimeSpan duration = new TimeSpan();
            string img = null;
            string webDataUrl = baseUrlDrNu + "/programcard/expanded/" + itemslug;
            string strprogramcard = GetWebData(webDataUrl);
            JObject objprogramcard = JObject.Parse(strprogramcard);
            string itemTitle = (string)objprogramcard["Data"][0]["Broadcasts"][0]["Title"];
            string itemDescription = (string)objprogramcard["Data"][0]["Broadcasts"][0]["Description"];
            JArray assets = (JArray)objprogramcard["Data"][0]["Assets"];
            if (assets.Count > 0)
            {
              for (int iasset = 0; iasset < assets.Count; iasset++)
              {
                string kind = (string)assets[iasset]["Kind"];
                string uri = (string)assets[iasset]["Uri"];
                if (kind == "VideoResource")
                {
                  link = loadAsset(uri);
                  duration = TimeSpan.FromMilliseconds((int)assets[iasset]["DurationInMilliseconds"]);
                }
                if (kind == "Image")
                {
                  img = uri;
                }
              }
            }
            if (link.Length > 0)
            {
              VideoInfo video = new VideoInfo();
              video.Title = itemTitle;
              video.Description = itemDescription;
              video.VideoUrl = link;
              video.Length = duration.ToString();
              video.ImageUrl = img;
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

    public override List<VideoInfo> getVideoList(Category category)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      string[] myString = category.Other.ToString().Split(',');
      if (myString[0] == "drlive")
      {
        return getlivestreams();
      }

      if (myString[0] == "search")
      {

        string url = baseUrlDrNu + "/programcard?Title=$like(\"" + myString[1] + "\")&limit=$eq(50)";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getvideosSearch(contentData);
      }

      if (myString[0] == "drnulist_card")
      {
        string url = baseUrlDrNu + "/programcard?Relations.Slug=\"" + myString[1] + "\"&limit=$eq(50)";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getvideosAlpha(contentData);
      }

      if (myString[0] == "drnupremiere")
      {
        string url = baseUrlDrNu + "/bundle/forpremierer";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getvideosPremiere(contentData);
      }

      if (myString[0] == "drnumostviewed")
      {
        string url = baseUrlDrNu + "/View/programviews?days=7&count=10";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getvideos(contentData);
      }

      if (myString[0] == "drnuspotlight")
      {
        string url = baseUrlDrNu + "/bundle/test-spotliste";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getvideosSpot(contentData);
      }

      if (myString[0] == "drnuhighlight")
      {
        string url = baseUrlDrNu + "/bundle/hoejdepunkter";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        return getvideosSpot(contentData);
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
        string[] alpha = new string[29] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "Æ", "Ø", "Å" };
        for (int i = 0; i < 29; i++)
        {

          RssLink subCategory = new RssLink()
          {
            Name = alpha[i],
            ParentCategory = parentCategory,
            HasSubCategories = true,
            SubCategoriesDiscovered = false,
            Other = "drnulist_alpha," + alpha[i]
          };
          parentCategory.SubCategories.Add(subCategory);
        }
      }

      if (myString[0] == "drnulist_alpha")
      {
        string url = baseUrlDrNu + "/view/bundles-with-public-asset?Title=$like(\"" + myString[1] + "\"),$orderby(\"asc\")&BundleType=$eq(\"Series\")&ChannelType=$eq(\"TV\")&limit=100";
        string json = GetWebData(url);
        JObject contentData = JObject.Parse(json);
        if (contentData != null)
        {
          JArray slugs = (JArray)contentData["Data"];
          for (int i = 0; i < slugs.Count; i++)
          {
            try
            {
              JArray broadcasts = (JArray)contentData["Data"][i]["ProgramCard"]["Broadcasts"];
              if (broadcasts.Count > 0)
              {
                string itemslug = slugs[i].Value<string>("Slug");
                string itemTitle = slugs[i].Value<string>("Title");
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

        //Add static category premiere
        subCategory = new RssLink()
        {
          Name = "Forpremiere",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          Other = "drnupremiere,"
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

        //Add static category spotlight
        subCategory = new RssLink()
        {
          Name = "Spotlight",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 3,
          Other = "drnuspotlight,"
        };
        parentCategory.SubCategories.Add(subCategory);

        //Add static category highlight
        subCategory = new RssLink()
        {
          Name = "Højdepunkter",
          ParentCategory = parentCategory,
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          EstimatedVideoCount = 6,
          Other = "drnuhighlight,"
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

    public override List<VideoInfo> Search(string query)
    {
      Category search = new Category();
      search.Other = "search," + query;
      return getVideoList(search);
    }

    #endregion

  }
}
