using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.georgius
{
    internal class ShowEpisodesRegex
    {
        #region Private fields
        #endregion

        #region Constructors

        public ShowEpisodesRegex()
        {
            this.ShowNames = new List<string>();
            this.ShowEpisodesBlockEndRegex = String.Empty;
            this.ShowEpisodesBlockStartRegex = String.Empty;
            this.ShowEpisodeLengthRegex = String.Empty;
            this.ShowEpisodeStartRegex = String.Empty;
            this.ShowEpisodeEndRegex = String.Empty;
            this.ShowEpisodeThumbUrlRegex = String.Empty;
            this.ShowEpisodeUrlAndTitleRegex = String.Empty;
            this.ShowEpisodesNextPageRegex = String.Empty;
            this.ShowEpisodeDescriptionRegex = String.Empty;
            this.ShowEpisodeDateRegex = String.Empty;
            this.SkipFirstPage = false;
        }

        #endregion

        #region Properties

        public List<String> ShowNames { get; set; }
        public String ShowEpisodesBlockStartRegex { get; set; }
        public String ShowEpisodesBlockEndRegex { get; set; }
        public String ShowEpisodeStartRegex { get; set; }
        public String ShowEpisodeEndRegex { get; set; }
        public String ShowEpisodeThumbUrlRegex { get; set; }
        public String ShowEpisodeLengthRegex { get; set; }
        public String ShowEpisodeUrlAndTitleRegex { get; set; }
        public String ShowEpisodeDescriptionRegex { get; set; }
        public String ShowEpisodesNextPageRegex { get; set; }
        public String ShowEpisodeDateRegex { get; set; }
        public Boolean SkipFirstPage { get; set; }

        #endregion
    }
}
