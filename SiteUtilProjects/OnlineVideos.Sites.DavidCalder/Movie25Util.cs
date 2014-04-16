using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public class Movie25Util : DeferredResolveUtil
  {
    public override ITrackingInfo GetTrackingInfo(VideoInfo video)
    {
      try
      {
        TrackingInfo tInfo = new TrackingInfo()
        {
          Regex = Regex.Match(video.Title, "(?<Title>[^(]*)((?<Airdate>.*))"),
          VideoKind = VideoKind.Movie
        };

      }
      catch (Exception e)
      {
        Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
      }

      return base.GetTrackingInfo(video);
    }

    public override string ResolveVideoUrl(string url)
    {
      string newUrl = url;
      string webData = GetWebData(newUrl);
      Match match = Regex.Match(webData, @"onclick=""location.href='(?<url>[^']*)'""\s*value=""Click\sHere\sto\sPlay""", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        newUrl = match.Groups["url"].Value;
      }
      Log.Info(newUrl);
      return GetVideoUrl(newUrl);
    }

    public override bool CanSearch
    {
      get
      {
        return true;
      }
    }

    public override List<ISearchResultItem> DoSearch(string query)
    {
      List<ISearchResultItem> cats = new List<ISearchResultItem>();

      Regex r = new Regex(@"<div\sclass=""movie_pic""><a\shref=""(?<url>[^""]*)""\starget=""_blank"">\s*<img\ssrc=""(?<thumb>[^""]*)""\swidth=""101""\sheight=""150""\s/>\s*</a></div>\s*<div\sclass=""movie_about"">\s*<div\sclass=""movie_about_text"">\s*<h1><a\shref=""[^""]*""\starget=""_blank"">(?<title>[^<]*)</a></h1>", defaultRegexOptions);

      query = query.Replace(" ", "+");
      string webData = GetWebData(string.Format(baseUrl + "/search.php?key={0}&submit=", query), forceUTF8: true);
      Match m = r.Match(webData);
      while (m.Success)
      {
        RssLink cat = new RssLink();
        cat.Url = baseUrl + m.Groups["url"].Value;
        if (!string.IsNullOrEmpty(dynamicSubCategoryUrlFormatString)) cat.Url = string.Format(dynamicSubCategoryUrlFormatString, cat.Url);
        cat.Url = ApplyUrlDecoding(cat.Url, dynamicSubCategoryUrlDecoding);
        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
        cat.Name = m.Groups["title"].Value.Trim();
        cat.Thumb = m.Groups["thumb"].Value;
        if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
        cat.Description = m.Groups["description"].Value;
        cat.HasSubCategories = false;
        cats.Add(cat);
        m = m.NextMatch();
      }
      return cats;
    }

    public override int DiscoverDynamicCategories()
    {
      base.DiscoverDynamicCategories();
      int i = 0;
      do
      {
        RssLink cat = (RssLink)Settings.Categories[i];
        if (cat.Name == "Submit Links" || cat.Name == "TV Shows")
          Settings.Categories.Remove(cat);
        else
        {
          i++;
        }
      }   
      while (i < Settings.Categories.Count);
      return Settings.Categories.Count;
    }
  }
}
