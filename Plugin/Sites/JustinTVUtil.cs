using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Web;

namespace OnlineVideos.Sites
{
    public class JustinTVUtil : SiteUtilBase
    {
        public enum rtype { unknown, recorded, live };        

        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://clipta.com/jtv_search/index_1.php?q={0}";

        public override string getUrl(VideoInfo video)
        {
            string str3;
            if (video.VideoUrl.IndexOf("www.justin.tv") > -1)
            {
                string webData = SiteUtilBase.GetWebData(video.VideoUrl);
                webData = webData.Substring(0, webData.IndexOf(".flv") + 4);
                return webData.Substring(webData.LastIndexOf("http:"), webData.Length - webData.LastIndexOf("http:"));
            }
            string str2 = str3 = "";
            DataSet set = new DataSet();
            set.ReadXml("http://live.justin.tv/find/live_user_" + video.VideoUrl + ".xml");
            DataTable table = set.Tables["node"];
            int num = 0;
            string token = "";
            while (num < table.Rows.Count)
            {
                str2 = table.Rows[num]["play"].ToString();
                str3 = table.Rows[num]["connect"].ToString();
                token = table.Rows[num]["token"].ToString();
                table.Rows[num]["token"].ToString().Substring(0, table.Rows[num]["token"].ToString().IndexOf(":"));
                break;
            }
            string str4 = str3.Replace("rtmp://", "").Replace("/app", "");
            // todo : send token as NetStream.Authenticate.UsherToken after connect packet in rtmp
            return string.Format("http://127.0.0.1:{6}/stream.flv?hostname={0}&port={1}&app={2}&swfUrl={3}&playpath={4}&tcUrl={5}&pageurl={7}&usefp9=true&authobj={8}&auth={9}&subscribepath={10}", new object[] { 
                HttpUtility.UrlEncode(str4), 
                "1935", 
                "app", 
                HttpUtility.UrlEncode("http://www-cdn.justin.tv/widgets/live_site_player.r7d3ed44c4594caafa272b91a2de339eb03325273.swf"), 
                HttpUtility.UrlEncode(str2), 
                HttpUtility.UrlEncode(str3), 
                OnlineVideoSettings.RTMP_PROXY_PORT,
                HttpUtility.UrlEncode("http://www.justin.tv/"+ video.VideoUrl),
                HttpUtility.UrlEncode("NetStream.Authenticate.UsherToken"),
                HttpUtility.UrlEncode(token),
                HttpUtility.UrlEncode(str2) });
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> list = new List<VideoInfo>();            
            DataSet set = new DataSet();            
            set.ReadXml(((RssLink)category).Url);
            DataTable table = set.Tables["channel"];
            DataTable table2 = set.Tables["stream"];
            for (int i = 0; i < table.Rows.Count; i++)
            {
                VideoInfo item = new VideoInfo();
                item.Description = "on " + table.Rows[i]["title"].ToString();
                item.ImageUrl = table.Rows[i]["screen_cap_url_medium"].ToString();
                item.Title = table2.Rows[i]["title"].ToString();
                if (item.Title.Length == 0)
                {
                    item.Title = "I am live right now!";
                }
                item.Length = "";
                item.VideoUrl = table.Rows[i]["login"].ToString();
                list.Add(item);
            }
            return list;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();            
            result.Add("Live", "live");
            result.Add("Recorded", "recorded");
            return result;
        }

        public override List<VideoInfo> Search(string query)
        {
            return Search(query, "unknown");
        }

        public override List<VideoInfo> Search(string query, string category)
        {
            rtype rt = (rtype)Enum.Parse(typeof(rtype), category);

            List<VideoInfo> list = new List<VideoInfo>();
            string webData = "";
            string str2 = "";
            string str3 = "";
            string str4 = "";
            string str5 = "";
            string str6 = "";
            string str7 = "";

            VideoInfo info;
            if ((rt == rtype.unknown) || (rt == rtype.live))
            {
                webData = SiteUtilBase.GetWebData(string.Format(searchUrl, query) + "&type[]=live");
                if (webData.IndexOf("<LI class=\"sponsored_result\">") > -1)
                {
                    str3 = webData.Substring(webData.IndexOf("<LI class=\"sponsored_result\">") + 0x1c, (webData.Length - 0x1c) - webData.IndexOf("<LI class=\"sponsored_result\">"));
                }
                else
                {
                    return list;
                }
                bool flag = false;
                if (str3.IndexOf("<LI class") > -1)
                {
                    flag = true;
                    str3 = str3.Substring(str3.IndexOf("<LI class") + 9, (str3.Length - str3.IndexOf("<LI class")) - 9);
                }
                if (flag)
                {
                    rt = rtype.live;
                }
            }
            if (rt == rtype.live)
            {
                while (true)
                {
                    try
                    {
                        str4 = str3.Substring(str3.IndexOf("src=\"") + 5, (str3.Length - str3.IndexOf("src=\"")) - 5);
                        str4 = str4.Substring(0, str4.IndexOf("\""));
                        str7 = str3.Substring(str3.IndexOf("class=\"description bold_results\">") + 0x22, (str3.Length - str3.IndexOf("class=\"description bold_results\">")) - 0x22).Replace("<b>", "").Replace("</b>", "");
                        str7 = str7.Substring(0, str7.IndexOf("<")).Trim();
                        str5 = str3.Substring(str3.IndexOf("class=\"title bold_results\"") + 0x1c, (str3.Length - str3.IndexOf("class=\"title bold_results\"")) - 0x1c).Replace("<b>", "").Replace("</b>", "");
                        str5 = str5.Substring(0, str5.IndexOf("<")).Trim();
                        str6 = str4.Substring(str4.IndexOf("live_user_") + 10, (str4.Length - str4.IndexOf("live_user_")) - 10);
                        str6 = str6.Substring(0, str6.IndexOf("-"));
                    }
                    catch (Exception)
                    {
                        return list;
                    }
                    info = new VideoInfo();
                    info.Description = "on " + str5;
                    info.ImageUrl = str4;
                    info.Title = str7;
                    if (info.Title.Length == 0)
                    {
                        info.Title = "I am live right now!";
                    }
                    info.Length = "";
                    info.VideoUrl = str6;
                    list.Add(info);
                    if (str3.IndexOf("<LI class") == -1)
                    {
                        return list;
                    }
                    str3 = str3.Substring(str3.IndexOf("<LI class") + 9, (str3.Length - str3.IndexOf("<LI class")) - 9);
                }
            }
            webData = SiteUtilBase.GetWebData(string.Format(searchUrl, query) + "&type[]=recorded");
            if (webData.IndexOf("<LI class=\"sponsored_result\">") > -1)
            {
                str2 = webData.Substring(webData.IndexOf("<LI class=\"sponsored_result\">") + 0x1c, (webData.Length - 0x1c) - webData.IndexOf("<LI class=\"sponsored_result\">"));
            }
            else
            {
                return list;
            }
            bool flag2 = false;
            if (str2.IndexOf("<LI class") > -1)
            {
                flag2 = true;
                str2 = str2.Substring(str2.IndexOf("<LI class") + 9, (str2.Length - str2.IndexOf("<LI class")) - 9);
            }
            while (flag2)
            {
                try
                {
                    str4 = str2.Substring(str2.IndexOf("\" src=\"") + 7, (str2.Length - str2.IndexOf("\" src=\"")) - 7);
                    str4 = str4.Substring(0, str4.IndexOf("\""));
                    str7 = str2.Substring(str2.IndexOf("class=\"description bold_results\">") + 0x22, (str2.Length - str2.IndexOf("class=\"description bold_results\">")) - 0x22).Replace("<b>", "").Replace("</b>", "");
                    str7 = str7.Substring(0, str7.IndexOf("<")).Trim();
                    str5 = str2.Substring(str2.IndexOf("class=\"title bold_results\"") + 0x1c, (str2.Length - str2.IndexOf("class=\"title bold_results\"")) - 0x1c).Replace("<b>", "").Replace("</b>", "");
                    str5 = str5.Substring(0, str5.IndexOf("<")).Trim();
                    str6 = str2.Substring(str2.IndexOf("<A href=\"") + 9, (str2.Length - str2.IndexOf("<A href=\"")) - 9);
                    str6 = str6.Substring(0, str6.IndexOf("\""));
                }
                catch (Exception)
                {
                    return list;
                }
                info = new VideoInfo();
                info.Description = "on " + str5;
                info.ImageUrl = str4;
                info.Title = str7;
                if (info.Title.Length == 0)
                {
                    info.Title = "I am live right now!";
                }
                info.Length = "";
                info.VideoUrl = str6;
                list.Add(info);
                if (str2.IndexOf("<LI class") == -1)
                {
                    return list;
                }
                str2 = str2.Substring(str2.IndexOf("<LI class") + 9, (str2.Length - str2.IndexOf("<LI class")) - 9);
            }
            return list;            
        }

        #endregion
    }
}