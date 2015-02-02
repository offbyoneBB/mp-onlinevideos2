using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.RoChess
{
	public class CountryMusicTelevisionUtil : GenericSiteUtil
	{
		[Category("OnlineVideosConfiguration"), Description("regex used to parse the playlist file url from the config file of the flash player")]
		string configFileRegEx;

		public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
		{
            string resolvedUrl = WebCache.Instance.GetRedirectedUrl(playlistUrl);
			string page = HttpUtility.ParseQueryString(new Uri(resolvedUrl).Query)["CONFIG_URL"];
			string config = GetWebData(page, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
			playlistUrl = Regex.Match(config, configFileRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture).Groups["url"].Value;
			return base.GetPlaybackOptions(HttpUtility.HtmlDecode(playlistUrl));
		}
	}
}
