using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXPlaylist
    {
        bool ready = false;
        public bool Ready { get { return ready; } }

        double version = 0;
        public double Version { get { return version; } }

        string logo = "";
        public string Logo { get { return logo; } }

        string title = "";
        public string Title { get { return title; } }

        string description = "";
        public string Description { get { return description; } }

        string url = "";
        public string URL { get { return url; } }

        List<NaviXMediaItem> items = new List<NaviXMediaItem>();
        public List<NaviXMediaItem> Items { get { return items; } }

        public NaviXPlaylist(string url)
        {
            this.url = url;
            string plTxt = OnlineVideos.Sites.SiteUtilBase.GetWebData(url);
            parsePlaylist(plTxt);
        }

        void parsePlaylist(string plTxt)
        {
            string[] plData = plTxt.Split("\r\n".ToCharArray());
            if (plData.Length < 1)
                return;

            int state = 0;
            int prevState = 0;
            NaviXMediaItem currentItem = null;

            foreach (string m in plData)
            {
                int index;
                switch (state)
                {
                    case 2: //Parsing playlist description
                        index = m.IndexOf("/description");
                        if (index > -1)
                        {
                            description += "\n" + m.Remove(index);
                            state = 0;
                        }
                        else
                            description += "\n" + m;
                        continue;
                    case 3: //Parsing item description
                        index = m.IndexOf("/description");
                        if (index > -1)
                        {
                            description += "\n" + m.Remove(index);
                            state = 1;
                        }
                        else
                            description += "\n" + m;
                        continue;
                    case 4: //multiline comment
                        if (m.StartsWith("\"\"\""))
                            state = prevState;
                        continue;
                }

                if (m.Length < 1 || m[0] == '#')
                    continue;

                if (m.StartsWith("\"\"\"")) //Start of multiline comment
                {
                    prevState = state;
                    state = 4;
                    continue;
                }

                index = m.IndexOf('=');
                if (index < 0)
                    continue;

                string key = m.Remove(index);
                string value = m.Substring(index + 1);

                if (state == 0)
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
                                state = 2;
                            }
                            continue;
                        case "type":
                            currentItem = new NaviXMediaItem();
                            currentItem.Type = value;
                            state = 1;
                            continue;
                    }
                }
                else if (state == 1)
                {
                    switch (key)
                    {
                        case "version":
                            double lVersion;
                            if (double.TryParse(value, out lVersion))
                                currentItem.Version = lVersion;
                            continue;
                        case "type":
                            items.Add(currentItem);
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
                                state = 3;
                            }
                            continue;
                    }
                }
            }

            if (state == 1 || prevState == 1)
                items.Add(currentItem);
            ready = items.Count > 0;
        }
    }
}
