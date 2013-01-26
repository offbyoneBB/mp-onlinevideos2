using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using OnlineVideos.Sites.Pondman.Nodes;

namespace OnlineVideos.Sites.Pondman.ITunes.Nodes {

    public class Video : ExternalContentNodeBase, IVideoDetails
    {

        public string Title {get; set;}
        public DateTime Published { get; set; }
        public TimeSpan Duration { get; set; }
		public string ThumbUrl { get; set; }

        public Dictionary<VideoQuality, Uri> Files {
            get {
                
                if (this.files == null) 
                {
                    this.files = new Dictionary<VideoQuality, Uri>();
                }

                return this.files;
            }
        } Dictionary<VideoQuality, Uri> files;

        /// <summary>
        /// Updates the video information with the available qualities
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        public override NodeResult Update()
        {
            Video video = this;

			if (string.IsNullOrEmpty(video.Uri)) // if no Uri is set, we cannot update data
			{
				if (video.Files.Count > 0) // data was alread discovered in an earlier step
				{
					if (video.State == NodeState.Initial) video.state = NodeState.Complete; // mark this video as complete
					return NodeResult.Success;
				}
				else
				{
					return NodeResult.Failed;
				}
			}

            string url = video.Uri;
            string data = this.session.MakeRequest(video.Uri);
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root == null)
            {
                return NodeResult.Failed;
            }

            // Filter the playlist nodes to get the ones we need
            HtmlNodeCollection playlist = root.FirstChild.SelectNodes("//array/dict");

            bool metadata = false;
            if (playlist.Count > 0)
            {
                foreach (HtmlNode clip in playlist)
                {
                    // Get the node following the previewURL this is the video url of the trailer
                    string videourl = clip.SelectSingleNode("key[contains(.,'previewURL')]").NextSibling.InnerText;

                    // Filter the Ipod format
                    if (!videourl.Contains(".m4v"))
                    {
                        // Get the node following the songName this is the title of the trailer
                        string title = clip.SelectSingleNode("key[contains(.,'songName')]").NextSibling.InnerText;

                        // Get the node following the previewLength this is the duration of the video in seconds
                        int duration = int.Parse(clip.SelectSingleNode("key[contains(.,'previewLength')]").NextSibling.InnerText);

                        // get the release date of this specific trailer
                        string date = clip.SelectSingleNode("key[contains(.,'releaseDate')]").NextSibling.InnerText;

                        string label;
                        VideoQuality quality;

                        // Parse label and quality from the provided title
                        Video.ParseAppleVideoTitle(title, out label, out quality);

                        // Add the current quality to the video
                        video.Files[quality] = new Uri(videourl);

                        //  Update the metadata (only one time)
                        if (metadata)
                        {
                            continue;
                        }

                        // Set the duration of the video
                        video.Duration = new TimeSpan(0, 0, duration);

                        if (!String.IsNullOrEmpty(date))
                        {
                            DateTime dt;
                            if (DateTime.TryParse(date, out dt))
                            {
                                video.Published = dt;
                            }
                        }

                        metadata = true;
                    }
                }

            }

            this.state = NodeState.Complete;
            return NodeResult.Success;
        }

        static void ParseAppleVideoTitle(string input, out string title, out VideoQuality quality) {
            // This parses a title formatted as: "Trailer Name (Quality)" 
            // from the playlist and splits it into a title and the quality enum

            string[] parts = input.Split('(');
            if (parts.Length == 2) {
                // this should be the title.
                title = parts[0].Trim();
                // this should be the quality identifier
                string q = parts[1].Replace(")", "").ToLower();
                switch (q) {
                    case "small":
                        quality = VideoQuality.Small;
                        break;
                    case "medium":
                        quality = VideoQuality.Medium;
                        break;
                    case "large":
                        quality = VideoQuality.Large;
                        break;
                    case "hd 480p":
                        quality = VideoQuality.HD480;
                        break;
                    case "hd 720p":
                        quality = VideoQuality.HD720;
                        break;
                    case "hd 1080p":
                        quality = VideoQuality.FullHD;
                        break;
                    default:
                        quality = VideoQuality.Unknown;
                        break;
                }
            }
            else {
                title = input;
                quality = VideoQuality.Unknown;
            }
        }

		public static VideoQuality ParseVideoQuality(string text)
		{
			if (text.ToLowerInvariant().Contains("480p"))
				return VideoQuality.HD480;
			if (text.ToLowerInvariant().Contains("720p"))
				return VideoQuality.HD720;
			if (text.ToLowerInvariant().Contains("1080p"))
				return VideoQuality.FullHD;
			if (text.ToLowerInvariant().Contains("small") || text.ToLowerInvariant().Contains("iphone"))
				return VideoQuality.Small;
			if (text.ToLowerInvariant().Contains("medium"))
				return VideoQuality.Medium;
			if (text.ToLowerInvariant().Contains("large"))
				return VideoQuality.Large;

			return VideoQuality.Unknown;
		}

        #region IVideoDetails Members

        public Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Date", Published != DateTime.MinValue ? Published.ToShortDateString() : "N/A");
            return properties;
        }

        #endregion

    }
}
