using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Xml;
using System.ComponentModel;
using System.Threading;

namespace OnlineVideos.Sites
{
  public class TV2Util : SiteUtilBase
  {

    string tv2regex = @"'(?<title>[^']*)':\[(?<content>[^\[]*)]";
    Regex regextv2;

    public override void Initialize(SiteSettings siteSettings)
    {
      base.Initialize(siteSettings);
      if (!string.IsNullOrEmpty(tv2regex)) regextv2 = new Regex(tv2regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
    }

    public override string GetVideoUrl(VideoInfo video)
    {
      XmlDocument doc = new XmlDocument();
      string redirectUrl = GetWebData("http://common.tv2.dk/flashplayer/playlist.xml.php/alias-player_news/autoplay-1/clipid-" + video.Id + "/keys-NEWS,PLAYER.xml");
      doc.LoadXml(redirectUrl);
      XmlNodeList elemList = doc.GetElementsByTagName("source");
        string attrVal = elemList[0].Attributes["video"].Value;
        return attrVal;
    }


    public override List<VideoInfo> GetVideos(Category category)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      XmlDocument doc = new XmlDocument();
      string json = GetWebData(category.Other.ToString());
      JArray contentData = JArray.Parse(json);
      if (contentData != null)
      {
        foreach (var item in contentData)
        {
          VideoInfo video = new VideoInfo();
          video.Id = item.Value<int>("id");
          video.Title = item.Value<string>("title");
          video.Description = item.Value<string>("description");
          video.Thumb = item.Value<string>("img");
          string air = item.Value<string>("time");
          res.Add(video);
        }
      }
      return res;
    }

    public override int DiscoverDynamicCategories()
    {
      string[] paths = new string[9] { "nyheder", "most-viewed", "nyh0600", "nyh0900", "nyh1700", "nyh1900", "nyh2200", "station2", "newsmagasiner" };
      string[] names = new string[9] { "Nyheder", "Mest sete", "06:00", "09:00", "17:00", "19:00", "22:00", "Station 2", "Newsmagasiner" };

      for (int i = 0; i < 9; i++)
      {

        RssLink mainCategory = new RssLink()
        {
          Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[i]),
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          Other = "http://nyhederne.tv2.dk/video/data/tag/" + paths[i] + "/",
          Url = "http://nyhederne.tv2.dk/video/data/tag/" + paths[i] + "/"
        };
        Settings.Categories.Add(mainCategory);

      }
      Settings.DynamicCategoriesDiscovered = true;
      return Settings.Categories.Count;
    }
  }
}
