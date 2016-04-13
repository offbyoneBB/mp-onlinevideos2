using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Helper.TvLogoDB
{

    public class LogoChannel
    {
        public static LogoChannel[] GetChannel(string channel)
        {
            List<LogoChannel> oReturn = new List<LogoChannel>();
            try 
            {
                string request = "http://www.thelogodb.com/api/json/v1/2112/tvchannel.php?s=" + channel.ToLower().Trim().Replace(" ", "_");
                string sContent = WebCache.Instance.GetWebData(request);
                JObject tObj = JObject.Parse(sContent);
                JArray tArray = (JArray)tObj["channels"];
                foreach (JObject item in tArray)
                {
                    LogoChannel newItem = new LogoChannel()
                    {
                        LogoWide = (string)item["strLogoWide"]
                    };
                    oReturn.Add(newItem);
                }
            }
            catch { }

            return oReturn.ToArray();
        }

        public string IdChannel { get; set; }
        public string Name{get;set;}
        public string PackageIDs{get;set;}
        public string Lyngsat{get;set;}
        public string Country{get;set;}
        public string LyngsatLogo{get;set;}
        public string LogoWide{get;set;}
        public string LogoWideBW{get;set;}
        public string LogoSquare{get;set;}
        public string LogoSquareBW{get;set;}
        public string Fanart1{get;set;}
        public string Description{get;set;}
        public string dateModified{get;set;}
    }
}
