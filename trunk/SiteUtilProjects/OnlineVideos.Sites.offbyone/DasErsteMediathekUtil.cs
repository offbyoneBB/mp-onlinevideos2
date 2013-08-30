using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{    
    public class DasErsteMediathekUtil : GenericSiteUtil
    {        
        public enum VideoQuality { Low, High, Max };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName="VideoQuality"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.High;

		[Category("OnlineVideosConfiguration")]
		string SendungVerpasst_baseUrl = "http://www.ardmediathek.de/ard/servlet/ajax-cache/3551682/view=module/index.html";
		[Category("OnlineVideosConfiguration")]
		string SendungVerpasst_dynamicSubCategoriesRegEx = @"<li[^>]*><a\s+href=""/sendung-verpasst\?(?<url>datum=[^""]+)""[^>]*>(?<title>.*?)</a></li>";
		[Category("OnlineVideosConfiguration")]
		string SendungVerpasst_dynamicSubCategoryUrlFormatString = @"/ard/servlet/ajax-cache/3517242/view=list/{0}/senderId=208/zeit=1/index.html";

		public override int DiscoverDynamicCategories()
		{
			int result = base.DiscoverDynamicCategories();
			Settings.Categories.Add(new RssLink() { Name = "Sendung verpasst?", HasSubCategories = true, Url = SendungVerpasst_baseUrl });
            //Settings.Categories.Add(new Category() { Name = "Live" });
			return result + 1;
		}

		public override int DiscoverSubCategories(Category parentCategory)
		{
			if (parentCategory.Name == "Sendung verpasst?")
			{
				parentCategory.SubCategories = new List<Category>();
				var m = Regex.Match(GetWebData((parentCategory as RssLink).Url), SendungVerpasst_dynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
				while (m.Success)
				{
					RssLink cat = new RssLink();
					cat.Url = m.Groups["url"].Value;
					cat.Url = string.Format(SendungVerpasst_dynamicSubCategoryUrlFormatString, cat.Url);
					cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
					cat.Name = Utils.PlainTextFromHtml(System.Web.HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim()));
					cat.ParentCategory = parentCategory;
					parentCategory.SubCategories.Add(cat);
					m = m.NextMatch();
				}
				parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
				return parentCategory.SubCategories.Count;
			}
			else
			{
				return base.DiscoverSubCategories(parentCategory);
			}
		}

        /*public override List<VideoInfo> getVideoList(Category category)
        {
            if (category is RssLink)
                return base.getVideoList(category);
            else
                return new List<VideoInfo>() 
                { 
                    new VideoInfo()
                    {
                         Title = "Das Erste - Live Stream",
                         VideoUrl = "http://daserste_live-lh.akamaihd.net/z/daserste_de@91204/manifest.f4m?hdcore=2.11.4&g=" + Utils.GetRandomLetters(12)
                    }
                };
        }*/

        public override String getUrl(VideoInfo video)
        {
            /*if (video.Title == "Das Erste - Live Stream")
                return video.VideoUrl;
            */
            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
            {
                string dataPage = GetWebData(video.VideoUrl);
                video.PlaybackOptions = new Dictionary<string, string>();
                Match match = regEx_FileUrl.Match(dataPage);
                List<string[]> options = new List<string[]>();
                while (match.Success)
                {
                    string[] infos = match.Groups["Info"].Value.Split(',');
                    for (int i = 0; i < infos.Length; i++) infos[i] = infos[i].Trim(new char[] { '"', ' ' });
                    options.Add(infos);
                    match = match.NextMatch();
                }
                options.Sort(new Comparison<string[]>(delegate(string[] a, string[] b)
                    {
                        return int.Parse(a[1]).CompareTo(int.Parse(b[1]));
                    }));
                foreach(string[] infos in options)
                {
                    int type = int.Parse(infos[0]);
                    VideoQuality quality = (VideoQuality)int.Parse(infos[1]);
                    string resultUrl = "";
                    if (infos[infos.Length - 3].ToLower().StartsWith("rtmp"))
                    {
						resultUrl = new MPUrlSourceFilter.RtmpUrl(infos[infos.Length - 3].Replace("rtmpt://", "rtmp://")) { PlayPath = infos[infos.Length - 2].Trim(new char[] { '"', ' ' }) }.ToString();
                        video.PlaybackOptions.Add(string.Format("{0} | rtmp:// | {1}", quality.ToString().PadRight(4, ' '), infos[infos.Length - 2].ToLower().Contains("mp4:") ? ".mp4" : ".flv"), resultUrl);
                    }
                    else
                    {
                        resultUrl = infos[infos.Length - 2].Trim(new char[] { '"', ' ' });                        
                        if (!resultUrl.EndsWith(".mp3"))
                        {
                            try
                            {
                                Uri uri = new Uri(resultUrl);
                                video.PlaybackOptions.Add(string.Format("{0} | {1}:// | {2}", quality.ToString().PadRight(4, ' '), uri.Scheme, System.IO.Path.GetExtension(resultUrl)), uri.AbsoluteUri);
                                if (resultUrl.EndsWith(".asx"))
                                {
                                    resultUrl = ParseASX(resultUrl)[0];
                                    uri = new Uri(resultUrl);
                                    video.PlaybackOptions.Add(string.Format("{0} | {1}:// | {2}", quality.ToString().PadRight(4, ' '), uri.Scheme, System.IO.Path.GetExtension(resultUrl)), uri.AbsoluteUri);
                                }                            
                            }
                            catch { }
                        }
                    }
                }
            }

            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
            {
				// no url to play available
                return "";
            }
            else if (video.PlaybackOptions.Count == 1 || videoQuality == VideoQuality.Low)
            {
                //user wants low quality or only one playback option -> use first
                return video.PlaybackOptions.First().Value;
            }
            else if (videoQuality == VideoQuality.Max)
            {
                // take highest available quality
				return video.PlaybackOptions.Last().Value;
            }
            else
            {
				// choose a high quality from options (first below Max)
				return video.PlaybackOptions.Last(v => !v.Key.StartsWith(VideoQuality.Max.ToString())).Value;
            }
        }

    }
}

