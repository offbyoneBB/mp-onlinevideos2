using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{    
    public class RTLNowUtil : SiteUtilBase
    {
        protected string configName = "RTLNowNetworkConfig";
        private Dictionary<char, string> criticals = new Dictionary<char, string>();

        protected string base64Decode(string data)
        {
            string str2;
            try
            {
                data = data.Substring(0x36);
                Decoder decoder = new UTF8Encoding().GetDecoder();
                byte[] bytes = Convert.FromBase64String(data);
                char[] chars = new char[decoder.GetCharCount(bytes, 0, bytes.Length)];
                decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
                string str = new string(chars);
                str2 = "x" + str;
            }
            catch (Exception exception)
            {
                throw new Exception("Error in base64Decode" + exception.Message);
            }
            return str2;
        }

        protected string ConvertUmlaut(string strIN)
        {
            return strIN.Replace("\x00c4", "Ae").Replace("\x00e4", "ae").Replace("\x00d6", "Oe").Replace("\x00f6", "oe").Replace("\x00dc", "Ue").Replace("\x00fc", "ue");
        }

        protected string convertUnicodeD(string source)
        {
            int startIndex = 0;
            do
            {
                startIndex = source.IndexOf("&#", startIndex);
                if (startIndex > 0)
                {
                    int num3;
                    int index = source.IndexOf(";", startIndex);
                    int.TryParse(source.Substring(startIndex + 2, (index - startIndex) - 2), out num3);
                    source = source.Replace(source.Substring(startIndex, (index - startIndex) + 1), char.ConvertFromUtf32(num3).ToString());
                }
            }
            while (startIndex > 0);
            return source;
        }

        protected string convertUnicodeU(string source)
        {
            int startIndex = 0;
            do
            {
                startIndex = source.IndexOf(@"\u", startIndex);
                if (startIndex > 0)
                {
                    int num2;
                    int.TryParse(source.Substring(startIndex + 2, 4), NumberStyles.HexNumber, null, out num2);
                    source = source.Replace(source.Substring(startIndex, 6), char.ConvertFromUtf32(num2).ToString());
                }
            }
            while (startIndex > 0);
            return source;
        }

        protected string getCachedHTMLData(string fsUrl, string postData, int page)
        {
            string source = GetWebData(fsUrl);
            source = this.convertUnicodeD(source);
            return this.convertUnicodeU(source);
        }

        public string getFileNameForRecord(VideoInfo link)
        {
            return (this.ConvertUmlaut(link.Title) + ".asf");
        }

        private string getLink(string source)
        {
            List<string> list = new List<string>();
            int beginIndex = 0;
            do
            {
                string str = "";
                beginIndex = getTagValues(source, "<button", "highlight_button(this);'>", out str, beginIndex);
                if (((beginIndex > 0) && (str.IndexOf("id=\"bt_dsl") >= 0)) && (str.IndexOf("visibility:hidden;") == -1))
                {
                    getTagValues(str, "onclick='movie=\"", "\";", out str, 0);
                    list.Add(str);
                }
            }
            while (beginIndex >= 0);
            return list[list.Count - 1];
        }

        protected static int getTagValues(string source, string begTag, string endTag, out string value, int beginIndex)
        {
            int num;
            value = "";
            if (begTag != null)
            {
                num = source.IndexOf(begTag, beginIndex);
            }
            else
            {
                num = beginIndex;
            }
            if (num < 0)
            {
                return -1;
            }
            if (begTag != null)
            {
                num += begTag.Length;
            }
            int index = source.IndexOf(endTag, num);
            if (index < 0)
            {
                return -1;
            }
            value = source.Substring(num, index - num);
            return (index + endTag.Length);
        }

        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            string fsId = video.VideoUrl;
            fsId = "http://rtl-now.rtl.de/" + fsId;
            new XmlDocument();
            string source = GetWebData(fsId);
            string str2 = GetWebData(this.getLink(source));
            int startIndex = str2.IndexOf("<Ref href = \"mms") + 13;
            return str2.Substring(startIndex, str2.IndexOf("\"/>", startIndex) - startIndex);
        }


        protected void getVideoListFromScreen(string fsUrl, List<VideoInfo> listOfLinks, string postData, int page)
        {
            string source = this.getCachedHTMLData(fsUrl, postData, page);
            int beginIdx = 0;
            int num2 = -1;
            if (getTagValues(source, "id=\"list_xajax_content\"", "class=\"formatfooter\"", out source, 0) > 0)
            {
                while (true)
                {
                    string name = "";
                    beginIdx = this.GetVideoName(source, beginIdx, out name);
                    if (beginIdx == -1)
                    {
                        break;
                    }
                    string time = "";
                    beginIdx = this.GetVideoTime(source, beginIdx, out time);
                    if (beginIdx == -1)
                    {
                        break;
                    }
                    VideoInfo item = new VideoInfo();
                    beginIdx = this.GetVideoUrl(source, beginIdx, out item.VideoUrl);
                    if (beginIdx == -1)
                    {
                        break;
                    }
                    name = ConvertUTF8(name + "  " + time);
                    item.Description = HttpUtility.HtmlDecode(name);
                    item.Title = item.Description;
                    listOfLinks.Add(item);
                    num2 = beginIdx;
                }
            }
            beginIdx = num2;
            if ((page == 0) && (beginIdx > 0))
            {
                string str4 = "javascript:xajax_show_top_and_movies(1,'";
                beginIdx = source.IndexOf(str4, beginIdx) + str4.Length;
                int index = source.IndexOf("'", beginIdx);
                string str5 = source.Substring(beginIdx, index - beginIdx);
                while (beginIdx > str4.Length)
                {
                    page++;
                    postData = string.Concat(new object[] { "xajax=show_top_and_movies&xajaxr=", DateTime.Now.ToFileTime(), "&xajaxargs[]=", page, "&xajaxargs[]=", str5, "&xajaxargs[]=reiter1&xajaxargs[]=0&xajaxargs[]=0&xajaxargs[]=0" });
                    this.getVideoListFromScreen(fsUrl, listOfLinks, postData, page);
                    str4 = "javascript:xajax_show_top_and_movies(" + (page + 1) + ",'";
                    beginIdx = source.IndexOf(str4, beginIdx) + str4.Length;
                }
            }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsUrl = (category as RssLink).Url;
            List<VideoInfo> listOfLinks = new List<VideoInfo>();
            this.getVideoListFromScreen(fsUrl, listOfLinks, "", 0);
            return listOfLinks;
        }

        protected int GetVideoName(string source, int beginIdx, out string name)
        {
            string str = "";
            int num = getTagValues(source, "<div class=\"title\"", "</div>", out str, beginIdx);
            getTagValues(source, "<a href=\"", "</a>", out str, beginIdx);
            name = str.Substring(str.IndexOf(">") + 1);
            return num;
        }

        protected int GetVideoTime(string source, int beginIdx, out string time)
        {
            return getTagValues(source, "<div class=\"time\">", "</div>", out time, beginIdx);
        }

        protected int GetVideoUrl(string source, int beginIdx, out string url)
        {
            string str = "";
            int num = getTagValues(source, "<div class=\"buy\">", "</div>", out str, beginIdx);
            getTagValues(str, "<a href=", ">", out url, 0);
            return num;
        }

        public static string ConvertUTF8(string html)
        {
            int startIndex = 0;
            while (true)
            {
                startIndex = html.IndexOf(@"\u", 0);
                if (startIndex == -1)
                {
                    return html;
                }
                startIndex += 2;
                string s = html.Substring(startIndex, 4);
                html = html.Replace(@"\u" + s, ((char)((ushort)short.Parse(s, NumberStyles.AllowHexSpecifier))).ToString());
            }
        }
    }
}

/*
    <Site name="RTL Now" util="RTLNow" agecheck="false" enabled="true">
      <Username />
      <Password />
      <SearchUrl />
      <Categories>
        <Category xsi:type="RssLink" name="Ahornallee"><![CDATA[http://rtl-now.rtl.de/ahorn.php]]></Category>
        <Category xsi:type="RssLink" name="Alarm fuer Cobra 11"><![CDATA[http://rtl-now.rtl.de/cobra.php]]></Category>
        <Category xsi:type="RssLink" name="Alles was Zaehlt"><![CDATA[http://rtl-now.rtl.de/alles_was_zaehlt.php]]></Category>
        <Category xsi:type="RssLink" name="Anwaelte der Toten"><![CDATA[http://rtl-now.rtl.de/anwaeltedertoten.php]]></Category>
        <Category xsi:type="RssLink" name="Bauer sucht Frau"><![CDATA[http://rtl-now.rtl.de/bauersuchtfrau.php]]></Category>
        <Category xsi:type="RssLink" name="Boese Maedchen"><![CDATA[http://rtl-now.rtl.de/boesemaedchen.php]]></Category>
        <Category xsi:type="RssLink" name="Die Autohaendler"><![CDATA[http://rtl-now.rtl.de/autohaendler.php]]></Category>
        <Category xsi:type="RssLink" name="Die Kinderarzte von St.Marien"><![CDATA[http://rtl-now.rtl.de/die_kinderaerzte_von_st_marien.php]]></Category>
        <Category xsi:type="RssLink" name="Die Oliver Geissen Show"><![CDATA[http://rtl-now.rtl.de/oliver_geissen.php]]></Category>
        <Category xsi:type="RssLink" name="Einer gegen Hundert"><![CDATA[http://rtl-now.rtl.de/einergegenhundert.php]]></Category>
        <Category xsi:type="RssLink" name="Familienhilfe mit Herz"><![CDATA[http://rtl-now.rtl.de/familienhilfe.php]]></Category>
        <Category xsi:type="RssLink" name="Im Namen des Gesetzes"><![CDATA[http://rtl-now.rtl.de/indg.php]]></Category>
        <Category xsi:type="RssLink" name="Mario Barth praesentiert: Die besten Comedians Deutschlands"><![CDATA[http://rtl-now.rtl.de/mariobarth.php]]></Category>
        <Category xsi:type="RssLink" name="Mein Baby"><![CDATA[http://rtl-now.rtl.de/mein_baby.php]]></Category>
        <Category xsi:type="RssLink" name="Mein Garten"><![CDATA[http://rtl-now.rtl.de/meingarten.php]]></Category>
        <Category xsi:type="RssLink" name="Mitten im Leben"><![CDATA[http://rtl-now.rtl.de/mildoku.php]]></Category>
        <Category xsi:type="RssLink" name="Natascha Zuraw"><![CDATA[http://rtl-now.rtl.de/zuraw.php]]></Category>
        <Category xsi:type="RssLink" name="Staatsanwalt Posch ermittelt"><![CDATA[http://rtl-now.rtl.de/posch.php]]></Category>
        <Category xsi:type="RssLink" name="Unglaublich! Die Show der Merkwuerdigkeiten"><![CDATA[http://rtl-now.rtl.de/unglaublich.php]]></Category>
        <Category xsi:type="RssLink" name="Unsere erste gemeinsame Wohnung"><![CDATA[http://rtl-now.rtl.de/unserewohnung.php]]></Category>
        <Category xsi:type="RssLink" name="Unser neues Zuhause"><![CDATA[http://rtl-now.rtl.de/unserneueszuhause.php]]></Category>
        <Category xsi:type="RssLink" name="Guten Abend RTL"><![CDATA[http://rtl-now.rtl.de/gutenabendrtl.php]]></Category>
        <Category xsi:type="RssLink" name="RTL Aktuell"><![CDATA[http://rtl-now.rtl.de/rtl_aktuell.php]]></Category>
        <Category xsi:type="RssLink" name="RTL News Kompakt"><![CDATA[http://rtl-now.rtl.de/rtl_news.php]]></Category>
        <Category xsi:type="RssLink" name="RTL Exclusiv"><![CDATA[http://rtl-now.rtl.de/exclusiv.php]]></Category>
        <Category xsi:type="RssLink" name="RTL Exclusiv Kompakt"><![CDATA[http://rtl-now.rtl.de/exclusivkompakt.php]]></Category>
        <Category xsi:type="RssLink" name="RTL Exclusiv Spezial"><![CDATA[http://rtl-now.rtl.de/exclusiv_spezial.php]]></Category>
        <Category xsi:type="RssLink" name="RTL Explosiv"><![CDATA[http://rtl-now.rtl.de/explosiv.php]]></Category>
        <Category xsi:type="RssLink" name="RTL Extra Kompakt"><![CDATA[http://rtl-now.rtl.de/extrakompakt.php]]></Category>
        <Category xsi:type="RssLink" name="Hoellische Nachbarn"><![CDATA[http://rtl-now.rtl.de/hoellischenachbarn.php]]></Category>
        <Category xsi:type="RssLink" name="Die Supernanny"><![CDATA[http://rtl-now.rtl.de/nanny.php]]></Category>
        <Category xsi:type="RssLink" name="Raus aus den Schulden"><![CDATA[http://rtl-now.rtl.de/rads.php]]></Category>
      </Categories>
    </Site>
*/