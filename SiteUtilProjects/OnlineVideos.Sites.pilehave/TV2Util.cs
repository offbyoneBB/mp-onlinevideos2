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

    public override string getUrl(VideoInfo video)
    {
      XmlDocument doc = new XmlDocument();
      string redirectUrl = GetWebData("http://common.tv2.dk/flashplayer/playlistSimple.xml.php/clip-" + video.Id + ".xml");
      doc.LoadXml(redirectUrl);
      XmlNodeList elemList = doc.GetElementsByTagName("source");
        string attrVal = elemList[0].Attributes["video"].Value;
        return attrVal;
    }


    public override List<VideoInfo> getVideoList(Category category)
    {
      List<VideoInfo> res = new List<VideoInfo>();
      XmlDocument doc = new XmlDocument();
      string json = category.Other.ToString();
      JArray contentData = JArray.Parse('[' + json + ']');
      if (contentData != null)
      {
        foreach (var item in contentData)
        {
          VideoInfo video = new VideoInfo();
          video.Id = item.Value<int>("id");
          video.Title = item.Value<string>("headline");
          video.Description = item.Value<string>("descr");
          video.ImageUrl = item.Value<string>("img");
          string len = item.Value<string>("duration");
          string air = item.Value<string>("date");
          video.Length = len + '|' + Translation.Airdate + ": " + air;
          res.Add(video);
        }
      }
      return res;
    }

    public override int DiscoverDynamicCategories()
    {
      string js = GetWebData("http://video.tv2.dk/js/video-list.js.php/index.js");
      Match m = regextv2.Match(js);
      while (m.Success)
      {
        RssLink mainCategory = new RssLink()
        {
          Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Groups["title"].Value),
          HasSubCategories = false,
          SubCategoriesDiscovered = false,
          Other = m.Groups["content"].Value,
          Url = "http://www.dr.dk/live"
        };
        Settings.Categories.Add(mainCategory);
        m = m.NextMatch();
      }
      Settings.DynamicCategoriesDiscovered = true;
      return Settings.Categories.Count;
    }
  }
}
