using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public class CouchtunerUtil : DeferredResolveUtil
  {
    public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
    {
      Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
      string webData = GetWebData(playlistUrl);
      Match m = Regex.Match(webData, @"<(iframe\ssrc|IFRAME\sSRC)=""(?<m0>[^""]*)""[^>]*>");
      //<iframe src="http://vidup.me/embed-wfdtsamhfipc-540x330.html"

      while (m.Success)
      {
        string url = m.Groups["m0"].Value.Replace("-", "/");
        Match n = Regex.Match(url, @"(http://www.|http://)(?<n0>[^/]*)/embed/(?<m0>[^/]*)[^""]*");
        if (m.Success && n.Success)
        {
          playbackOptions.Add(n.Groups["n0"].Value, string.Format("http://{0}/{1}", n.Groups["n0"].Value, n.Groups["m0"].Value));
        }
        m = m.NextMatch();
      }
      return playbackOptions;
    }

    public override int DiscoverDynamicCategories()
    {
      base.DiscoverDynamicCategories();
      int i = 0;
      do
      {
        RssLink cat = (RssLink)Settings.Categories[i];
        if (cat.Name == "How To Watch")
        {
          Settings.Categories.Remove(cat);
        }
        else if (cat.Name == "TV Show List")
        {
          cat.HasSubCategories = true;
        }
        else if (cat.Name == "New Release")
        {
          cat.HasSubCategories = false;
        }
        i++;
      }
      while (i < Settings.Categories.Count);
      return Settings.Categories.Count;
    }

    public override int DiscoverSubCategories(Category parentCategory)
    {
      if (parentCategory.Name == "TV Show List")
      {
        base.DiscoverSubCategories(parentCategory);
        foreach (Category cat in parentCategory.SubCategories)
        {
          cat.HasSubCategories = true;
        }
      }
      else if (parentCategory.Name == "0-9" || parentCategory.Name.Length == 1)
      {
        string data = GetWebData(baseUrl + "tv-streaming/");

        string letter;
        if (parentCategory.Name == "0-9")
          letter = "z1";
        else letter = parentCategory.Name.ToLower();

        Match n = Regex.Match(data, @"(?s)<div\sid=""" + letter + @"""[^>]*>.*?</div>");
        if (n.Success)
        {
          parentCategory.SubCategories = new List<Category>();
          Match m = Regex.Match(n.Value, @"<a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""\s>(?<title>[^<]*)</a>");
          while (m.Success)
          {
            RssLink newCat = new RssLink();
            newCat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
            newCat.Url = m.Groups["url"].Value;
            newCat.SubCategoriesDiscovered = false;
            newCat.HasSubCategories = false;
            parentCategory.SubCategories.Add(newCat);
            parentCategory.SubCategoriesDiscovered = true;
            newCat.ParentCategory = parentCategory;
            m = m.NextMatch();
          }
        }
      }
      else
      {
        RssLink cat = (RssLink)parentCategory;
        parentCategory.SubCategories = new List<Category>();

        string webData = GetWebData(cat.Url);

        Match n = Regex.Match(webData, @"<a\shref=""(?<url>[^""]*)""\srel=""bookmark"">\s*(?<title>[^<]*)</a></span><br/>");

        while (n.Success)
        {
          RssLink newCat = new RssLink();
          newCat.Name = HttpUtility.HtmlDecode(n.Groups["title"].Value);
          newCat.Url = n.Groups["url"].Value;
          parentCategory.SubCategoriesDiscovered = true;
          newCat.HasSubCategories = false;
          parentCategory.SubCategories.Add(newCat);
          newCat.ParentCategory = parentCategory;
          n = n.NextMatch();
        }
      }
      return parentCategory.SubCategories.Count;
    }

    public override List<VideoInfo> getVideoList(Category category)
    {
      List<VideoInfo> VideoList = base.getVideoList(category);
      if (VideoList.Count < 1)
      {
        RssLink cat = (RssLink)category;

        string webData = GetWebData(cat.Url);

        Match n = Regex.Match(webData, @"<a\shref=""(?<url>[^""]*)""\srel=""bookmark"">\s*(?<title>[^<]*)</a></span><br/>");

        while (n.Success)
        {
          VideoInfo newCat = new VideoInfo();
          newCat.Title = HttpUtility.HtmlDecode(n.Groups["title"].Value);
          newCat.VideoUrl = n.Groups["url"].Value;
          VideoList.Add(newCat);
          n = n.NextMatch();
        }
      }
      return VideoList;
    }
  }
}
