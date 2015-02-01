using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXPlaylist
    {
        static System.Text.RegularExpressions.Regex colorTagReg = new System.Text.RegularExpressions.Regex(@"\[/?COLOR[^\]]*\]", System.Text.RegularExpressions.RegexOptions.Compiled);

        double version = 0;
        public double Version { get { return version; } }

        string logo = "";
        public string Logo { get { return logo; } }

        string title = "";
        public string Title { get { return title; } }

        string description = "";
        public string Description { get { return description; } }
        
        List<NaviXMediaItem> items = new List<NaviXMediaItem>();
        public List<NaviXMediaItem> Items { get { return items; } }
        
        public static NaviXPlaylist Load(string url, string nxId)
        {
            CookieContainer cc = null;
            if (url.StartsWith("http://www.navixtreme.com") && !string.IsNullOrEmpty(nxId))
            {
                cc = new CookieContainer();
                cc.Add(new Cookie("nxid", nxId, "/", "www.navixtreme.com"));
            }

            string playlistText = OnlineVideos.Sites.SiteUtilBase.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(playlistText))
                return new NaviXPlaylist(playlistText);
            return null;
        }

        public NaviXPlaylist(string playlistText)
        {
            parsePlaylist(playlistText);
        }

        void parsePlaylist(string playlistText)
        {
            string[] lines = playlistText.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
                return;

            ParseState state = ParseState.PlaylistInfo;
            ParseState prevState = ParseState.PlaylistInfo;
            NaviXMediaItem currentItem = null;

            for (int x = 0; x < lines.Length; x++)
            {
                string m = lines[x];
                int index;
                switch (state)
                {
                    case ParseState.PlaylistDescription: //Parsing playlist description
                        index = m.IndexOf("/description");
                        if (index > -1)
                        {
                            description += "\n" + m.Remove(index);
                            state = ParseState.PlaylistInfo;
                        }
                        else
                            description += "\n" + m;
                        continue;
                    case ParseState.ItemDescription: //Parsing item description
                        index = m.IndexOf("/description");
                        if (index > -1)
                        {
                            currentItem.Description += "\n" + m.Remove(index);
                            state = ParseState.ItemInfo;
                        }
                        else
                            currentItem.Description += "\n" + m;
                        continue;
                    case ParseState.Comment: //multiline comment
                        if (m.StartsWith("\"\"\""))
                            state = prevState;
                        continue;
                }

                m = m.Trim();
                if (m.Length < 1 || m.StartsWith("#"))
                    continue;

                if (m.StartsWith("\"\"\"")) //Start of multiline comment
                {
                    prevState = state;
                    state = ParseState.Comment;
                    continue;
                }

                index = m.IndexOf('=');
                if (index < 0)
                    continue;

                string key = m.Remove(index);
                string value = m.Substring(index + 1);

                if (state == ParseState.PlaylistInfo)
                {
                    switch (key)
                    {
                        case "version":
                            double lVersion;
                            if (double.TryParse(value, out lVersion))
                                version = lVersion;
                            continue;
                        case "logo":
                            logo = value;
                            continue;
                        case "title":
                            title = value;
                            continue;
                        case "description":
                            index = value.IndexOf("/description");
                            if (index > -1)
                                description = value.Remove(index);
                            else
                            { //Multi-line description
                                description = value;
                                state = ParseState.PlaylistDescription;
                            }
                            continue;
                        case "type":
                            currentItem = new NaviXMediaItem();
                            currentItem.Type = value;
                            state = ParseState.ItemInfo;
                            continue;
                    }
                }
                else if (state == ParseState.ItemInfo)
                {
                    switch (key)
                    {
                        case "version":
                            double lVersion;
                            if (double.TryParse(value, out lVersion))
                                currentItem.Version = lVersion;
                            continue;
                        case "type":
                            addItem(currentItem);
                            currentItem = new NaviXMediaItem();
                            currentItem.Type = value;
                            continue;
                        case "name":
                            currentItem.Name = value;
                            continue;
                        case "date":
                            currentItem.Date = value;
                            continue;
                        case "thumb":
                            currentItem.Thumb = value;
                            continue;
                        case "icon":
                            currentItem.Icon = value;
                            continue;
                        case "URL":
                            currentItem.URL = value;
                            continue;
                        case "DLloc":
                            currentItem.DLloc = value;
                            continue;
                        case "player":
                            currentItem.Player = value;
                            continue;
                        case "background":
                            currentItem.Background = value;
                            continue;
                        case "rating":
                            currentItem.Rating = value;
                            continue;
                        case "infotag":
                            currentItem.InfoTag = value;
                            continue;
                        case "view":
                            currentItem.View = value;
                            continue;
                        case "processor":
                            currentItem.Processor = value;
                            continue;
                        case "playpath":
                            currentItem.PlayPath = value;
                            continue;
                        case "swfplayer":
                            currentItem.SWFPlayer = value;
                            continue;
                        case "pageurl":
                            currentItem.PageURL = value;
                            continue;
                        case "data":
                            currentItem.Data = value;
                            continue;
                        case "description":
                            index = value.IndexOf("/description");
                            if (index > -1)
                                currentItem.Description = value.Remove(index);
                            else
                            {
                                currentItem.Description = value;
                                state = ParseState.ItemDescription;
                            }
                            continue;
                    }
                }
            }

            if (state == ParseState.ItemInfo || prevState == ParseState.ItemInfo)
            {
                addItem(currentItem);
            }
        }

        void addItem(NaviXMediaItem item)
        {
            if (!string.IsNullOrEmpty(item.Name))
                item.Name = colorTagReg.Replace(item.Name, "");
            if (!string.IsNullOrEmpty(item.Description))
                item.Description = colorTagReg.Replace(item.Description, "");
            items.Add(item);
        }

        enum ParseState
        {
            PlaylistInfo,
            PlaylistDescription,
            ItemInfo,
            ItemDescription,
            Comment
        }
    }
}
