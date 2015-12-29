using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
	public partial class TF1Util : SiteUtilBase
	{
		protected int indexPage = 1;
		protected List<string> listPages = new List<string>();
		
		private static Regex videoIdByMediaRegex = new Regex(@"id=""media""\svalue=""(?<videoId>[^""]*)""",
														RegexOptions.Compiled);
		private static Regex videoIdByMediaIdRegex = new Regex(@"mediaId\s*:\s*(?<videoId>[^,]*),",
															   RegexOptions.Compiled);
		private static Regex videoIdByToolbarRegex = new Regex(@"new\sNewtoolbar\(""entry"",""(?<videoId>[^""]*)""",
															   RegexOptions.Compiled);


		internal string url_categories ="http://api.tf1.fr/tf1-genders/ipad/";
		internal string url_shows = "http://api.tf1.fr/tf1-programs/ipad/";
		internal string url_videos = "http://api.tf1.fr/tf1-vods/ipad/integral/1/program_id/";
		internal string url_videos2 = "http://api.tf1.fr/tf1-vods/ipad/integral/0/program_id/";


		public override int DiscoverDynamicCategories()
		{
			string webData = GetWebData(url_categories); 
			string parent="null";

			List<Category> tlist = GetCategories(webData, parent);

			foreach (Category item in tlist)
				Settings.Categories.Add(item);

			Settings.DynamicCategoriesDiscovered = true;

			return Settings.Categories.Count;
		}

		public override int DiscoverSubCategories(Category parentCategory)
		{
			parentCategory.SubCategories = new List<Category>();
			List<Category> tlist = new List<Category>();

			if (parentCategory.Other != "catalog")
			{
				string webData = GetWebData(url_categories);

				tlist = GetCategories(webData, (parentCategory as RssLink).Url);
				
			}
			else 
			{
				RssLink parent = parentCategory as RssLink;
				string webData = GetWebData(url_shows);
				List<Category> lst = GetCatalog(webData, (parentCategory as RssLink).Url);
				tlist.AddRange(lst.ToArray()); 
			}
			parentCategory.SubCategories.AddRange(tlist.ToArray());
			return tlist.Count;
   
		}

		public override List<VideoInfo> GetVideos(Category category)
		{
			RssLink cat = category as RssLink ;
			string webData = GetWebData("http://api.hd1.tv/hd1-vods/ipad/integral/1/program_id/" + category.Other);
			List<VideoInfo> tlist = GetVideo(webData, category.Other.ToString());
			return tlist;
		}

		public override string GetVideoUrl(VideoInfo video)
		{
			//http://www.wat.tv/get/web/12870773?token=b9c91b2466f02d56129c31c08bfe30c6/5630e01b&country=FR&getURL=1
			string sContent = GetWebData(video.VideoUrl);
			string surl = "http://wat.tv/get/ipad/12877475-audio%3D64000-video%3D601104.m3u8?vk=MTI4Nzc0NzUubTN1OA==&st=6Tq84JLf5kSRZQKj7X7W7w&e=1446343056&t=1446332256&min_bitrate=";
			return surl ;
		}


		public override bool HasNextPage
		{
			get
			{
				return listPages != null && listPages.Count > 1 && indexPage < listPages.Count;
			}
			protected set
			{
				base.HasNextPage = value;
			}
		}

		public override List<VideoInfo> GetNextPageVideos()
		{
			RssLink cat = new RssLink();
			indexPage++;
			cat.Url = listPages[indexPage - 1];
			return GetVideos(cat);
		}

		private List<Category> GetCategories(string webData, string parent)
		{

			List<Category> tReturn = new List<Category>();
			string pattern = "{\"id\":(?<id>[\\w]*),\"parentId\":" + parent + ",\"name\":\"(?<name>[\\w \\\\+\\/]*)\",\"childsCount\":(?<childs>[0-9]*)";
			Regex rgx = new Regex(pattern);
			var tmactchs = rgx.Matches(webData);

			foreach (Match item in tmactchs)
			{
				RssLink cat = new RssLink();
				cat.Url = item.Groups["id"].Value;
				cat.Name = item.Groups["name"].Value.Replace("\\u00e9", "é").Replace("\\u00e8", "è");
				cat.Other = "folder";
				string haschild = item.Groups["childs"].Value;
				int childs = 0;
				int.TryParse(haschild, out childs);
				if (childs == 0) 
				{
					cat.Other = "catalog";   
				}
				cat.HasSubCategories = true;
				tReturn.Add(cat);
			}

			return tReturn;
		}

		private List<VideoInfo> GetVideo(string webData, string genderID)
		{
			List<VideoInfo> tReturn = new List<VideoInfo>();

			JArray stuff = JArray.Parse(webData);
			//Infos program
			//http://www.wat.tv/interface/contentv4/12872393
			//Image
			//http://api.mytf1.tf1.fr/image/320/160/d9e68431-a725-41e4-a62d-5907fdefe931/c3c70c
			//Video playlist
			//http://www.wat.tv/get/web/12872393?token=0f0c610d0b7f5393b2e8567541c74f6b/5630c686&country=FR&getURL=1
		//http://3med.wat.tv/get/22b1bb4fe8cd54a9d91088667b988dec/5630c6ec/2/H264-384x288/23/93/12872393.h264?bu=&login=les-fees-cloches&i=92.150.59.229&u=47b58b95224c21f7f91191b9b77c580b&sum=b787fd2fa8e83dccaa3d07ce884ed74a&st=CgbV0852lUHnNduNXSsMIg&e=1446166828&t=1446037228&seek=wat


			
			foreach (var it in stuff) 
			{

					VideoInfo vid = new VideoInfo() 
					{
						Title = (string)it["shortTitle"]
						//VideoUrl = ((int)it["externalId"]).ToString() 
					};
					try 
					{
						Int32 id = ((int)it["watId"]);
						vid.VideoUrl = "http://wat.tv/get/ipad/"+id.ToString()+".m3u8";
					}
					catch
					{
					
					}
					try
					{
						vid.Thumb = (string)it["images"][0]["url"];
					}
					catch { }

					
					tReturn.Add(vid);
				
			} 


			//tReturn.AddRange(tres.ToArray()); 

			return tReturn;
		}

		private List<Category> GetCatalog(string webData, string genderID)
		{
			List<Category> tReturn = new List<Category>();

			JArray stuff = JArray.Parse(webData);
			//Infos program
			//http://www.wat.tv/interface/contentv4/12872393
			//Image
			//http://api.mytf1.tf1.fr/image/320/160/d9e68431-a725-41e4-a62d-5907fdefe931/c3c70c
			//Video playlist
			//http://www.wat.tv/get/web/12872393?token=0f0c610d0b7f5393b2e8567541c74f6b/5630c686&country=FR&getURL=1
			//http://3med.wat.tv/get/22b1bb4fe8cd54a9d91088667b988dec/5630c6ec/2/H264-384x288/23/93/12872393.h264?bu=&login=les-fees-cloches&i=92.150.59.229&u=47b58b95224c21f7f91191b9b77c580b&sum=b787fd2fa8e83dccaa3d07ce884ed74a&st=CgbV0852lUHnNduNXSsMIg&e=1446166828&t=1446037228&seek=wat



			foreach (var it in stuff)
			{
				if (((int)it["genderId"]).ToString() == genderID)
				{
					Category vid = new Category()
					{
						Name = (string)it["shortTitle"]
						//VideoUrl = ((int)it["externalId"]).ToString() 
					};
					try
					{
						Int32 id = ((int)it["id"]);
						vid.Other =  id.ToString();
					}
					catch
					{

					}
					try
					{
						vid.Thumb = (string)it["images"][0]["url"];
					}
					catch { }

					vid.HasSubCategories = false;
					tReturn.Add(vid);
				}
			}


			//tReturn.AddRange(tres.ToArray()); 

			return tReturn;
		}
		

		public static List<string> _getVideosUrl(VideoInfo video)
		{
			List<string> listUrls = new List<string>();

			string webData = WebCache.Instance.GetWebData(video.VideoUrl);
			string id = string.Empty;
			
			// check to see if videoId is in following format
			// <input type="hidden" id="media" value="10151643" />
			Match byMedia = videoIdByMediaRegex.Match(webData);
			if (byMedia.Success) { id = byMedia.Groups["videoId"].Value; }
			
			if (string.IsNullOrEmpty(id))
			{
				// if videoId is still empty, check if videoId is in following format
				// mediaId : 2283580,
				Match byMediaId = videoIdByMediaIdRegex.Match(webData);
				if (byMediaId.Success) { id = byMediaId.Groups["videoId"].Value; }
			}
			
			if (string.IsNullOrEmpty(id))
			{
				// if videoId is still empty, check if videoId is in following format
				// new Newtoolbar("entry","9986091"
				Match byToolbar = videoIdByToolbarRegex.Match(webData);
				if (byToolbar.Success) { id = byToolbar.Groups["videoId"].Value; }
			}
			
			if (string.IsNullOrEmpty(id))
			{
				// if videoId is still empty, log warning and return
				Log.Warn(@"Could not find videoId for {0} at URL: {1}", video.Title, video.VideoUrl);                
				return listUrls;
			}

			//Récupération du json
			webData = WebCache.Instance.GetWebData("http://www.wat.tv/interface/contentv3/" + id);

			JObject j = JObject.Parse(webData);

			foreach (var jObject in j)
			{
				if (jObject.Key.Equals("media"))
				{
					if (jObject.Value["files"] as JArray != null)
					{
						//Parcours tous les fichiers 
						foreach (var jSubCategoryObject in jObject.Value["files"] as JArray)
						{
							string web = "";
							id = jSubCategoryObject.Value<string>("id");
							string hd = jSubCategoryObject.Value<string>("hasHD");
							if (hd != null && hd.ToLower().Equals("true"))
							{
								web = "webhd";

							}
							else
							{
								web = "web";
							}
							string timeToken = getTimeToken();
							string md5 = toMD52("9b673b13fa4682ed14c3cfa5af5310274b514c4133e9b3a81e6e3aba00912564/" + web + "/" + id + "" + timeToken);
							string finalURL = getFinalUrl(md5, "http://www.wat.tv/get/" + web + "/" + id, timeToken, id);
						   
							if (finalURL.StartsWith("http"))
							{
								listUrls.Add(new MPUrlSourceFilter.HttpUrl(finalURL) { UserAgent = OnlineVideoSettings.Instance.UserAgent}.ToString());
							}
							else
							{
								//listUrls.Add(new MPUrlSourceFilter.RtmpUrl(finalURL) { SwfUrl = "http://www.wat.tv/images/v30/PlayerWat.swf", SwfVerify = true, ReceiveDataTimeout = 40000 }.ToString());
								listUrls.Add(new MPUrlSourceFilter.RtmpUrl(finalURL) { SwfUrl = "http://www.wat.tv/images/v30/PlayerWat.swf", SwfVerify = true }.ToString());
							}
						}
						break;
					}

				}
			}

			return listUrls;
		}

		private static string getFinalUrl(string token, string url, string timeToken, string id)
		{
			string webData = WebCache.Instance.GetWebData(url + "?domain=videos.tf1.fr&country=FR&getURL=1&version=LNX%2010,0,45,2&token=" + token + "/" + timeToken, userAgent: "Mozilla/5.0 (Windows; U; Windows NT 6.1; de; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3");
			if (webData.Contains("rtmpte://"))
			{
				webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmpe");
			}
			if (webData.Contains("rtmpe://"))
			{
				webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmpe");
			}
			if (webData.Contains("rtmp://"))
			{
				webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmp");
			}
			if (webData.Contains("rtmpt://"))
			{
				webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmp");
			}
			Log.Info("ReturnURL : " + webData);
			
			return webData;
		}

		private static string getTimeToken()
		{
			int delta = -3509;//delta temporaire entre mon PC et le serveur wat
			int time = Convert.ToInt32(GetTime() / 1000) + delta;
			string timesec = System.Convert.ToString(time, 16).ToLower();
			return timesec;
		}
   
		private static Int64 GetTime()
		{
			//Int64 retval=0;
		   
			//string s = GetWebData("http://wiilook.netau.net/script/time.php");
			//s = s.Substring(1, s.IndexOf("<") - 1);
			//return Int64.Parse(s);
			Int64 retval=0;
			DateTime st= new DateTime(1970,1,1);
			TimeSpan t= (DateTime.Now.ToUniversalTime()-st);
			retval = (Int64)(t.TotalMilliseconds + 0.5);
			return retval;
			//DateTime st= new DateTime(1970,1,1);
			//TimeSpan t= (DateTime.Now.ToUniversalTime()-st);
			//retval = (Int64)(t.TotalMilliseconds + 0.5);
			//return retval;
		}

		/// <summary>
		/// Creates an MD5 hash of the input string as a 32 character hexadecimal string.
		/// </summary>
		/// <param name="input">Text to generate has for.</param>
		/// <returns>Hash as 32 character hexadecimal string.</returns>
		public static string toMD52(string input)
		{
			System.Security.Cryptography.MD5 md5Hasher;
			byte[] data;
			int count;
			StringBuilder result;

			md5Hasher = System.Security.Cryptography.MD5.Create();
			data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

			// Loop through each byte of the hashed data and format each one as a hexadecimal string.
			result = new StringBuilder();
			for (count = 0; count < data.Length; count++)
			{
				result.Append(data[count].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
			}

			return result.ToString();
		}
	}
}
