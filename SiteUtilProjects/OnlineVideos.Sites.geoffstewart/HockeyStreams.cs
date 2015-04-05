/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 2/23/2013
 * Time: 1:33 PM
 * 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using OnlineVideos;
using OnlineVideos.Sites;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
	
	/// <summary>
	/// Implementation to support hockeystreams.com in OnlineVideos (MediaPortal)
	/// </summary>
	public class HockeyStreams : SiteUtilBase
	{
		#region UserConfiguration
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("HockeyStreams.com account username.")]
        string username = "";
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("HockeyStreams.com account password."), PasswordPropertyText(true)]
        string password = "";
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Days back for Archived Games"), Description("How many days back should the archived games go back?")]
        int archivedGamesDaysBackwards = 7;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Location (optional)"), Description("What location are you using?")]
        string location = "";
        #endregion
		
		// setters for username password for tests
		public void setUsername(string u) {
			username = u;
		}
		public void setPassword(string p) {
			password = p;
		}
		// unique key for this app on hockeystreams.com
		private static string apiKey = "d860817b09b1097731c4136b04a89444";
		
		private static string siteName = "HockeyStreams";
		
		// urls
		private const string apiUrl_login = @"http://api.hockeystreams.com/Login";
		private static string apiUrl_getLive = @"http://api.hockeystreams.com/GetLive";
		private static string apiUrl_getLiveStream = @"http://api.hockeystreams.com/GetLiveStream";
		private static string apiUrl_getOnDemand = @"http://api.hockeystreams.com/GetOnDemand";
		private static string apiUrl_getOnDemandStream = @"http://api.hockeystreams.com/GetOnDemandStream";
		private static string apiUrl_getCondensedGames = @"http://api.hockeystreams.com/GetCondensedGames";
		private static string apiUrl_getHighlights = @"http://api.hockeystreams.com/GetHighlights";
		
		// image urls
		private static string image_hs_url = @"http://www5.hockeystreams.com/images/logo.gif";
		private static string image_nhl_url = @"http://upload.wikimedia.org/wikipedia/en/thumb/3/3a/05_NHL_Shield.svg/200px-05_NHL_Shield.svg.png";

		// user-agent
		private static string useragent = "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:16.0.1) Gecko/20121011 Firefox/16.0.1";
		
		// static categories
		private static string liveGamesCategoryStr = "Live Games";
		private static string archivedGamesCategoryStr = "Archived Games";
		private static string condensedGamesCategoryStr = "Condensed Games";
		private static string highlightsCategoryStr = "Highlights";
		
				
		#region member variables
		private string token = "";
		private DateTime tokenCreateDate = DateTime.MinValue; 
		
		#endregion
		
		/*
		 * token expires after a day, but we'll use 12 hours
		 */
		private static int tokenResetDurationHours = -12;
		
		private string getAuthToken() {
			bool needNewToken = false;
			
			if (token == null || token.Equals("")) {
				needNewToken = true;	
				Log.Info("HockeyStreams - Need to login");
			} else if (tokenCreateDate == null || tokenCreateDate.AddHours(tokenResetDurationHours) > DateTime.Now) {
				// current token is more than 12 hours old
				needNewToken = true;
				Log.Info("HockeyStreams - Token has expired, need to login again");
			} 
			
			if (needNewToken) {
				string postData = string.Format("username={0}&password={1}&key={2}", username, password, apiKey);
			
				// get the authentication token
				String jsonResponse =  GetWebData(apiUrl_login,postData,null,null,null, false, false, useragent, null, null, false);
				
				JObject jsonLogin = JObject.Parse(jsonResponse);
				JToken authToken = new JValue("");
				bool yes = jsonLogin.TryGetValue("token",out authToken);
				token = authToken.ToString();
				tokenCreateDate = DateTime.Now;
			}
			
			return token;
			
			
		}
		
		/*
		 * Really only 2 categories: Live and Archived
		 */
		public override int DiscoverDynamicCategories()
		{
			
			Settings.Categories.Clear();
			
			Category liveGamesCategory = new Category();
			liveGamesCategory.Name = liveGamesCategoryStr;
			liveGamesCategory.Description = "These games will have streams today";
			Settings.Categories.Add(liveGamesCategory);
			liveGamesCategory.HasSubCategories = true;
			
			Category archivedGamesCategory = new Category();
			archivedGamesCategory.Name = archivedGamesCategoryStr;
			archivedGamesCategory.Description = "These are archived games";
			Settings.Categories.Add(archivedGamesCategory);
			archivedGamesCategory.HasSubCategories = true;

			Category condensedGamesCategory = new Category();
			condensedGamesCategory.Name = condensedGamesCategoryStr;
			condensedGamesCategory.Description = "These are condensed games";
			Settings.Categories.Add(condensedGamesCategory);
			condensedGamesCategory.HasSubCategories = true;
			
			Category highlightsCategory = new Category();
			highlightsCategory.Name = highlightsCategoryStr;
			highlightsCategory.Description = "These are highlights";
			Settings.Categories.Add(highlightsCategory);
			highlightsCategory.HasSubCategories = true;
			
			Settings.DynamicCategoriesDiscovered = false;
			
			return Settings.Categories.Count;
		}
		
		
		public override int DiscoverSubCategories(Category parentCategory)
        {
			parentCategory.SubCategories = new List<Category>();
              
			List<Category> nhlCategories = new List<Category>();
			List<Category> otherCategories = new List<Category>();
			
            if (parentCategory.Name.Equals(liveGamesCategoryStr)) {
            	
            	string authTokenString = getAuthToken();
				
				// get Live games
				string getLiveGamesUrl = string.Format("{0}?token={1}", apiUrl_getLive, authTokenString);
				
				string jsonResponse = GetWebData(getLiveGamesUrl,null,null,null,null, false, false, useragent, null, null, false);
			
				Log.Debug("Discover Sub Categories - Live Games: \\n" + jsonResponse);
				
				JObject jsonVideoList = JObject.Parse(jsonResponse);
				
				JToken jsonSchedule = jsonVideoList["schedule"];
				
				
				foreach (JToken game in jsonSchedule) {
					string feedType = game.Value<string>("feedType");
					if (string.IsNullOrEmpty(feedType) || feedType.Equals("null")) {
						feedType = "singleFeed";
					}
					
					LiveGame liveGame = new LiveGame();
					
					// encode ID into other
					string status = "";
					if (game.Value<string>("period").Length == 0) {
						status = game.Value<string>("startTime");
					} else {
						status = game.Value<string>("period");
					}
					liveGame.Other = game.Value<int>("id");
					liveGame.Name = string.Format("{0} - {1} {2} at {3} {4} - {5} - {6}",  
					                              game.Value<string>("event"), 
					                              game.Value<string>("awayTeam"), 
					                              game.Value<string>("awayScore"),
					                              game.Value<string>("homeTeam"), 
					                              game.Value<string>("homeScore"),
					                              status, 
					                              feedType);
					liveGame.ParentCategory = parentCategory;
					if (game.Value<string>("event").Equals("NHL")) {
						liveGame.Thumb = image_nhl_url;
						nhlCategories.Add(liveGame);
					} else {
						liveGame.Thumb = image_hs_url;
						otherCategories.Add(liveGame);
					}
					
				}
            } else if (parentCategory.Name.Equals(archivedGamesCategoryStr)){
            	string authTokenString = getAuthToken();
				
				// get archived games for today, yesterday and the day before that for 
				DateTime loopDate = DateTime.Now;
				
				for (int i = 0; i < archivedGamesDaysBackwards; i++) {
					// date string is formatted like this: MM/DD/YYYY
					string dateString = string.Format("{0:MM/dd/yyyy}", loopDate);
					
					loopDate = loopDate.AddDays(-1);
					
					string apiUrl = apiUrl_getOnDemand;
					
					if (parentCategory.Name.Equals(condensedGamesCategoryStr)) {
						apiUrl = apiUrl_getCondensedGames;
					} else if (parentCategory.Name.Equals(highlightsCategoryStr)) {
						apiUrl = apiUrl_getHighlights;
					}
				
					string fullUrl = string.Format("{0}?date={1}&token={2}", apiUrl, dateString, authTokenString);
				
					string jsonResponse =  GetWebData(fullUrl,null,null,null,null, false, false, useragent, null, null, false);
					
					if (jsonResponse.Length == 0) {
						continue;
					}
					
					Log.Debug("Discover Sub Categories - "+ parentCategory.Name + ": \\n" + jsonResponse);
					
					JObject jsonVideoList = JObject.Parse(jsonResponse);
					
					JToken jsonSchedule = jsonVideoList["ondemand"];
					
					if (parentCategory.Name.Equals(condensedGamesCategoryStr)) {
						jsonSchedule = jsonVideoList["condensed"];
					} else if (parentCategory.Name.Equals(highlightsCategoryStr)) {
						jsonSchedule = jsonVideoList["highlights"];
					}
					
					// condensed games and highlights get treated the same as archived games
					foreach (JToken game in jsonSchedule) {
						
						ArchivedGame archivedGame = new ArchivedGame();
						
						archivedGame.Other = game.Value<int>("id");
						// encode ID into other
						archivedGame.Name = string.Format("{0} - {1} - {2} at {3}",  
						                              game.Value<string>("event"), 
						                              game.Value<string>("date"),
						                              game.Value<string>("awayTeam"), 
						                              game.Value<string>("homeTeam")
						                              );
						archivedGame.ParentCategory = parentCategory;
						
						if (game.Value<string>("event").Equals("NHL")) {
							archivedGame.Thumb = image_nhl_url;
							nhlCategories.Add(archivedGame);
						} else {
							archivedGame.Thumb = image_hs_url;
							otherCategories.Add(archivedGame);
						}
						
						
					}
				}
									
				
            } else {
				// highlights and condensed games
				
							/*
			 * 
			 * structure of json doc
			 * 
* {"status":"Success","condensed":[
* {"id":"9981","date":"03\/30\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"Toronto Maple Leafs",
* 		"homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0513\/2_513_tor__1213_h_condensed_1600K_16x9_1.mp4","0":
* 		"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0513\/2_513_tor__1213_a_condensed_1600K_16x9_1.mp4"},
* {"id":"9931","date":"03\/28\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"New York Rangers","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0495\/2_495_nyr__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0495\/2_495_nyr__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9838","date":"03\/25\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"New Jersey Devils","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0473\/2_473_njd__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0473\/2_473_njd__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9762","date":"03\/23\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"Tampa Bay Lightning","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0456\/2_456_tbl__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0456\/2_456_tbl__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9713","date":"03\/21\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"Boston Bruins","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0447\/2_447_bos__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0447\/2_447_bos__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9676","date":"03\/19\/2013","event":"NHL","homeTeam":"New York Islanders","awayTeam":"Ottawa Senators","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0430\/2_430_ott__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0430\/2_430_ott__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9646","date":"03\/17\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"Winnipeg Jets","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0419\/2_419_wpg__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0419\/2_419_wpg__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9585","date":"03\/16\/2013","event":"NHL","homeTeam":"Buffalo Sabres","awayTeam":"Ottawa Senators","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0407\/2_407_ott__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0407\/2_407_ott__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9511","date":"03\/13\/2013","event":"NHL","homeTeam":"Montreal Canadiens","awayTeam":"Ottawa Senators","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0388\/2_388_ott__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0388\/2_388_ott__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9510","date":"03\/13\/2013","event":"NHL","homeTeam":"Montreal Canadiens","awayTeam":"Ottawa Senators","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0388\/2_388_ott__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0388\/2_388_ott__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9471","date":"03\/11\/2013","event":"NHL","homeTeam":"Ottawa Senators","awayTeam":"Boston Bruins","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0375\/2_375_bos__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0375\/2_375_bos__1213_a_condensed_1600K_16x9_1.mp4"},{"id":"9358","date":"03\/08\/2013","event":"NHL","homeTeam":"New York Rangers","awayTeam":"Ottawa Senators","homeSrc":"http:\/\/h264.nhl.com\/20122013\/02\/0350\/2_350_ott__1213_h_condensed_1600K_16x9_1.mp4","0":"awaySrc => http:\/\/h264.nhl.com\/20122013\/02\/0350\/2_350_ott__1213_a_condensed_1600K_16x9_1.mp4"}]}
			 */
				
            	string authTokenString = getAuthToken();
				
				// get archived games for today, yesterday and the day before that for 
				DateTime loopDate = DateTime.Now;
				
				for (int i = 0; i < archivedGamesDaysBackwards; i++) {
					// date string is formatted like this: MM/DD/YYYY
					string dateString = string.Format("{0:MM/dd/yyyy}", loopDate);
					
					loopDate = loopDate.AddDays(-1);
					
					string apiUrl = apiUrl_getOnDemand;
					
					apiUrl = apiUrl_getCondensedGames;
				
					string fullUrl = string.Format("{0}?date={1}&token={2}", apiUrl, dateString, authTokenString);
				
					string jsonResponse =  GetWebData(fullUrl,null,null,null,null, false, false, useragent, null, null, false);
					
					if (jsonResponse.Length == 0) {
						continue;
					}
					
					Log.Debug("Discover Sub Categories - "+ parentCategory.Name + ": \\n" + jsonResponse);
					
					JObject jsonVideoList = JObject.Parse(jsonResponse);
					
					JToken jsonSchedule = "";
					
					if (parentCategory.Name.Equals(condensedGamesCategoryStr)) {
						jsonSchedule = jsonVideoList["condensed"];
					} else if (parentCategory.Name.Equals(highlightsCategoryStr)) {
						jsonSchedule = jsonVideoList["highlights"];
					}
					
					// condensed games and highlights get treated the same as archived games
					foreach (JToken game in jsonSchedule) {
						
						DirectVideo dv = new DirectVideo();
						
						dv.Other = game.Value<int>("id");
						// encode ID into other
						dv.Name = string.Format("{0} - {1} - {2} at {3}",  
						                              game.Value<string>("event"), 
						                              game.Value<string>("date"),
						                              game.Value<string>("awayTeam"), 
						                              game.Value<string>("homeTeam")
						                              );
						dv.ParentCategory = parentCategory;
						dv.homeUrl = game.Value<string>("homeSrc");
						dv.awayUrl = game.Value<string>("awaySrc");
						
						if (game.Value<string>("event").Equals("NHL")) {
							dv.Thumb = image_nhl_url;
							nhlCategories.Add(dv);
						} else {
							dv.Thumb = image_hs_url;
							otherCategories.Add(dv);
						}
						
						
					}
				}
									
				
            } 
		

			
			// put NHL games first
			parentCategory.SubCategories.AddRange(nhlCategories);
			parentCategory.SubCategories.AddRange(otherCategories);
			
            Settings.DynamicCategoriesDiscovered = false;
            
            return parentCategory.SubCategories.Count;
		}
				
		private List<VideoInfo> getDirectVideoList(DirectVideo category) {
			List<VideoInfo> videoList = new List<VideoInfo>();
			
			VideoInfo h = new VideoInfo();
//			h.Id = 0;
			h.Description = category.Description;
			h.Title = category.Name;
			h.PlaybackOptions = new Dictionary<string, string>();
			if (category.homeUrl != null) {
				h.PlaybackOptions.Add("Home Feed", category.homeUrl);
			}
			if (category.awayUrl != null) {
				h.PlaybackOptions.Add("Away Feed", category.awayUrl);
			}
			videoList.Add(h);
			
			return videoList;
		}
		
		
		public override List<VideoInfo> GetVideos(Category category)
		{
			
			if (category.GetType().ToString().Equals("OnlineVideos.Sites.DirectVideo")) {
				return getDirectVideoList((DirectVideo)category);
			}
			
			List<VideoInfo> videoList = new List<VideoInfo>();
			
			string idForStream = category.Other.ToString();
			
			if (string.IsNullOrEmpty(idForStream)) {
				Log.Debug("Could not get id for stream, leaving");
			}
			
			string authTokenString = getAuthToken();
			
			// get Live games
			Boolean isOnDemand = false;
			string getStreamUrlPart = "";
			if (category.GetType().ToString().Equals("OnlineVideos.Sites.LiveGame")) {
				getStreamUrlPart = apiUrl_getLiveStream;
			} else {
				getStreamUrlPart = apiUrl_getOnDemandStream;
				isOnDemand = true;
			}
			string getStreamUrl = string.Format("{0}?id={1}&location={2}&token={3}", getStreamUrlPart, idForStream, location, authTokenString);
			
			string jsonResponse =  GetWebData(getStreamUrl,null,null,null,null, false, false, useragent, null, null, false);
			
			JObject jsonStreams = JObject.Parse(jsonResponse);
			
			
			if (jsonStreams != null &&
			    jsonStreams.Value<JToken>("HDstreams") != null) {
				int i = 0;
				string streamTag = "TrueLiveHD"; // for live games
				if (isOnDemand) {
					streamTag = "streams";
				}
				foreach(JToken stream in jsonStreams.Value<JToken>(streamTag)) {

					VideoInfo vi = new VideoInfo();
					
					Log.Debug("HockeyStreams - stream json : " + stream.ToString());
					
//					vi.Id = i++;
					vi.Title = string.Format("{0} - {1}", stream.Value<string>("type"), stream.Value<string>("location"));
					string url = stream.Value<string>("src");
					
					
//					if ("Flash".Equals(stream.Value<string>("type"))) {
//					if (stream.Value<string>("src").Contains("HD")) {
//						/*
//						 * URL looks like this:
//						 * http://174.127.101.96/vod2/HSTV4_03132013/PREMIUM_HSTV_4.f4m
//						 * 
//						 * Which gives back this doc with real urls:
//						 * <manifest>
//						 * 		<media href="http://174.127.101.96/hds-vod2/HSTV4_03132013/feed1.f4f.f4m" bitrate="500"/>
//						 *      <media href="http://174.127.101.96/hds-vod2/HSTV4_03132013/feed2.f4f.f4m" bitrate="1500"/>
//						 * </manifest>
//						 * 
//						 * OR (live game) like this:
//						 * URL: http://174.127.101.96/PREMIUM_HSTV_2.f4m
//						 * 
//						 * <manifest xmlns="http://ns.adobe.com/f4m/2.0"> 
//						 *      <baseURL>http://174.127.101.96/hds-live/publishing/PREMIUM_HSTV_2/liveevent/</baseURL> 
//						 *		<dvrInfo windowDuration="10800"/> 
//						 *		<media href="livestream1.f4m" bitrate="800"/> 
//						 *		<media href="livestream2.f4m" bitrate="1500"/> 
//						 *	</manifest> 
//						 */
//						XmlDocument xml = GetWebData<XmlDocument>(url);
//						XmlElement root = xml.DocumentElement;
//						XmlNodeList nodes = root.GetElementsByTagName("media");
//						XmlNodeList baseUrl = root.GetElementsByTagName("baseURL");
//						
//						string baseUrlStr = "";
//						if (baseUrl	!= null &&
//						    baseUrl.Item(0) != null) {
//							XmlNode baseUrlNode = baseUrl.Item(0);
//							baseUrlStr = baseUrlNode.InnerText;
//						}
//						
//						/*
//						 * populate the playback options so that a popup will prompt the user what bandwidth stream 
//						 * they want to watch.
//						 * 
//						 * getMultipleVideoUrl() is called after this and it will return the values in playback options
//						 */
//						vi.PlaybackOptions = new Dictionary<string, string>();
//						foreach(XmlNode node in nodes) {
//							string desc = "Flash - " + node.Attributes["bitrate"].Value + "k";
//							string vidUrl = baseUrlStr + node.Attributes["href"].Value;
//							vi.PlaybackOptions.Add(desc, vidUrl);
//							Log.Info("HockeyStreams - flash playback option vid url: " + vidUrl);
//						}
//							
//					} 
					
					Log.Info("HockeyStreams - URL for video in list: " + url);
					
					vi.VideoUrl = url;
					
					// skip options that don't have a URL
					if (url != null && url.Length > 0) {
						videoList.Add(vi);
					}
		
					
				}
			}
			
			return videoList;	
				
		}
		
		/**
		 * getVideoList() populates playback options for flash... 
		 * for a flash video, use the playback options, for others, just return the url
		 */
		public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist)
		{
			List<string> urlList = new List<string>();
			
			if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0) {
				// flash video will have playback options
				foreach(string vidUrl in video.PlaybackOptions.Values) {
					urlList.Add(vidUrl);
					Log.Info("Adding url to list {0}", vidUrl);
				}
			} else {
				// just return the URL
				urlList.Add(video.VideoUrl);
			}
			
				
			return urlList;
		
		}
		
		
	}
	

}