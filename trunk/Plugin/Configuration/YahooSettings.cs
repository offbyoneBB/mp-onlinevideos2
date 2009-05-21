using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Configuration;

namespace OnlineVideos
{
    public class YahooSettings
    {
        private int bandwith;

        public int Bandwith
        {
            get { return bandwith; }
            set { bandwith = value; }
        }

        private string user;

        public string User
        {
            get { return user; }
            set { user = value; }
        }

        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        private bool showPlayableOnly;

        public bool ShowPlayableOnly
        {
            get { return showPlayableOnly; }
            set { showPlayableOnly = value; }
        }

        private string locale;

        public string Locale
        {
            get { return locale; }
            set { locale = value; }
        }

        private string format_Title;

        public string Format_Title
        {
            get { return format_Title; }
            set { format_Title = value; }
        }

        private int itemCount;

        public int ItemCount
        {
            get { return itemCount; }
            set { itemCount = value; }
        }

        private string token;

        public string Token
        {
            get { return token; }
            set { token = value; }
        }

        private string favorites;

        public string Favorites
        {
            get { return favorites; }
            set { favorites = value; }
        }

        public void Load()
        {
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                Bandwith = xmlreader.GetValueAsInt("yahoomusic", "bandwith", 0);
                ItemCount = xmlreader.GetValueAsInt("yahoomusic", "itemcount", 25);
                Token = xmlreader.GetValueAsString("yahoomusic", "token", string.Empty);
                User = xmlreader.GetValueAsString("yahoomusic", "user", string.Empty);
                Password = xmlreader.GetValueAsString("yahoomusic", "password", string.Empty);
                ShowPlayableOnly = xmlreader.GetValueAsBool("yahoomusic", "showplayableonly", false);
                Locale = xmlreader.GetValueAsString("yahoomusic", "locale", "us");
                Favorites = xmlreader.GetValueAsString("yahoomusic", "favorites", string.Empty);
                Format_Title = xmlreader.GetValueAsString("yahoomusic", "format_title", "%artist% - %title%");
            }
        }

        public void Save()
        {
            using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                xmlwriter.SetValue("yahoomusic", "bandwith", Bandwith);
                xmlwriter.SetValue("yahoomusic", "itemcount", ItemCount);
                xmlwriter.SetValue("yahoomusic", "token", Token);
                xmlwriter.SetValue("yahoomusic", "user", User);
                xmlwriter.SetValue("yahoomusic", "password", Password);
                xmlwriter.SetValue("yahoomusic", "locale", Locale);
                xmlwriter.SetValue("yahoomusic", "format_title", Format_Title);
                xmlwriter.SetValue("yahoomusic", "favorites", Favorites);
                xmlwriter.SetValueAsBool("yahoomusic", "showplayableonly", ShowPlayableOnly);
            }
        }

        public YahooSettings()
        {
            ShowPlayableOnly = false;
            Bandwith = 0;
            ItemCount = 50;
            Favorites = string.Empty;
        }
    }
}
