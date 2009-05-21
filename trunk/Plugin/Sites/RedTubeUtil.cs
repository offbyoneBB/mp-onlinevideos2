using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class RedTubeUtil : SiteUtilBase
    {
        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(((RssLink)category).Url);
        }

        List<VideoInfo> Parse(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>();

            try
            {
                // receive main page
                string dataPage = GetData(fsUrl);
                Log.Debug("RedTube - Received " + dataPage.Length + " bytes");

                // is there any data ?
                if (dataPage.Length > 0)
                {
                    ParseLinks(dataPage, loRssItems);
                    if (loRssItems.Count > 0)
                    {
                        ParseThumbs(loRssItems);

                        Log.Debug("RedTube - finish to receive " + fsUrl);                        
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return loRssItems;
        }

        private string GetData(string Link)
        {
            try
            {
                int timeout = 5000;

                Cookie c = new Cookie();
                c.Name = "pp";
                c.Value = "1";
                c.Expires = DateTime.Now.AddHours(1);
                c.Domain = "www.redtube.com";

                CookieContainer cc = new CookieContainer();
                cc.Add(c);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Link);

                request.Timeout = timeout;
                request.CookieContainer = cc;

                string encodemap = "utf-8";
                WebResponse response = request.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding(encodemap);
                StreamReader reader = new StreamReader(receiveStream, encode);
                string str = reader.ReadToEnd();
                return str;
            }
            catch
            {
                return "";
            }
        }

        public void ParseLinks(string Data, List<VideoInfo> loRssItems)
        {
            int x = 0;
            int y = 0;

            while (x != -1)
            {
                x = Data.IndexOf("<a class=s", x);
                y = Data.IndexOf(">", x + 1);
                y = Data.IndexOf(">", y + 1);


                if ((y != -1) && (x != -1))
                {
                    string t = Data.Substring(x, y - x - 2);

                    // <a class=s target=_blank href='http://www.redtube.com/19791'>Brea Bennett alone on the couch<

                    int z = t.IndexOf("href=", 0);
                    if (z != -1)
                    {
                        y = t.IndexOf(">", z + 1);

                        string l = t.Substring(z + 5, y - z - 5);
                        l = l.Replace("'", "");
                        string d = t.Substring(y + 1, t.Length - y - 2);

                        z = l.LastIndexOf('/');

                        string no = "";
                        if (z != -1)
                        {
                            no = l.Substring(z + 1, l.Length - z - 1);
                        }

                        string flv = GetLink(no);

                        VideoInfo loRssItem = new VideoInfo();
                        loRssItem.Tags = l;
                        loRssItem.Title = d;
                        loRssItem.VideoUrl = flv;                        
                        loRssItem.SiteID = no;

                        loRssItems.Add(loRssItem);
                    }
                }
                if (x != -1)
                    x = x + 1;

            }
        }

        private void ParseThumbs(List<VideoInfo> loRssItems)
        {
            for (int i = 0; i < loRssItems.Count; i++)
            {
                VideoInfo loRssItem = loRssItems[i];
                loRssItem.ImageUrl = GetThumb(loRssItem.SiteID, 2);
            }
        }

        private string GetLink(string no)
        {
            string dl = "";
            Int64 nr = Convert.ToInt64(no);

            string[] map = {"R", "1", "5", "3", "4", "2", "O", "7",
                      "K", "9", "H", "B", "C", "D", "X", "F",
                      "G", "A", "I", "J", "8", "L", "M", "Z",
                      "6", "P", "Q", "0", "S", "T", "U", "V",
                      "W", "E", "Y", "N"};
            //int id = 19791;

            // org http://dl.redtube.com/_videos_t4vn23s9jc5498tgj49icfj4678/0000019/L9BB6X0ZX.flv?start=0

            // 1000

            string file = string.Format("{0:0000000}", nr);
            string leng = string.Format("{0:0000000}", nr / 1000);

            int value = 0;
            for (int i = 0; i < 7; i++)
            {
                value += (i + 1) * Convert.ToInt16(file[i] - 48);
            }
            string mv = value.ToString();

            value = 0;
            for (int i = 0; i < mv.Length; i++)
            {
                value += Convert.ToInt16(mv[i] - 48);
            }
            string qv = string.Format("{0:00}", value);

            string mapping = "";
            mapping = mapping + map[Convert.ToInt16(file[3]) - 48 + value + 3]; // char=0 map[48-48+3+3]=map[6] = "O"
            mapping = mapping + qv[1];                          // "3"
            mapping = mapping + map[Convert.ToInt16(file[0]) - 48 + value + 2]; // char=0 map[48-48+3+2]=map[5] = "2"
            mapping = mapping + map[Convert.ToInt16(file[2]) - 48 + value + 1]; // char=0 map[48-48+3+1]=map[4] = "4"
            mapping = mapping + map[Convert.ToInt16(file[5]) - 48 + value + 6]; // char=7 map[55-48+3+6]=map[16] = "G"
            mapping = mapping + map[Convert.ToInt16(file[1]) - 48 + value + 5]; // char=0 map[48-48+3+5]=map[8] = "K"
            mapping = mapping + qv[0];                          // "0"
            mapping = mapping + map[Convert.ToInt16(file[4]) - 48 + value + 7]; // char=4 map[4+3+7]=map[14] = "X"
            mapping = mapping + map[Convert.ToInt16(file[6]) - 48 + value + 4]; // char=7 map[7+3+4]=map[14] = "X"

            // L9BB6X0ZX

            dl = "http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//";
            dl += leng + "//";
            dl += mapping + ".flv";

            // neu http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//0000019//L9BB6X0ZX.flv
            // org http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//0000019//L9BB6X0ZX.flv?start=0

            // http://thumbs.redtube.com/_thumbs/0000019/0019791/0019791_016.jpg

            return dl;

        }

        private static int picNo = 2;

        public static string GetThumb(string no)
        {
            Int64 nr = Convert.ToInt64(no);

            picNo++;
            if ((picNo < 2) || (picNo > 16)) picNo = 2;

            string lnk = "";

            // http://thumbs.redtube.com/_thumbs/0000019/0019791/0019791_016.jpg
            // 2-16...

            lnk = "http://thumbs.redtube.com/_thumbs/";

            string file = string.Format("{0:0000000}", nr);
            string leng = string.Format("{0:0000000}", nr / 1000);
            string pic = "0" + string.Format("{0:00}", picNo);

            lnk += leng + "/" + file + "/" + file + "_" + pic + ".jpg";

            return lnk;
        }

        public static string GetThumb(string no, int selPic)
        {
            picNo = selPic;
            picNo--;
            return (GetThumb(no));
        }        
    }
}
