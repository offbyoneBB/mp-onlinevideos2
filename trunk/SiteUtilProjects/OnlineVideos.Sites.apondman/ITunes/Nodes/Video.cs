using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using OnlineVideos.Sites.Pondman.Nodes;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.Pondman.ITunes.Nodes {

    [Serializable]
    public class Video : ExternalContentNodeBase, IVideoDetails
    {
        public string Title { get; set; }
        public DateTime Published { get; set; }
        public TimeSpan Duration { get; set; }
		public string ThumbUrl { get; set; }
		public SerializableDictionary<VideoQuality, string> Files { get; set; }

		public Video()
		{
			Files = new SerializableDictionary<VideoQuality, string>();
		}

        /// <summary>
        /// Updates the video information with the available qualities
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        public override NodeResult Update()
        {
            Video video = this;

			if (string.IsNullOrEmpty(video.Uri) || video.Uri.StartsWith("file://")) // if no Uri (or our special uri) is set, we cannot update data
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

            HtmlNode asset = root.SelectSingleNode("//httpfilevideoasset");

            // media url
            HtmlNode mediaUrlNode = asset.SelectSingleNode("mediaurl");          
            if (mediaUrlNode == null) 
            {
                return NodeResult.Failed;
            }

            // image
            HtmlNode imageUrlNode = asset.SelectSingleNode("image");
            if (string.IsNullOrEmpty(video.ThumbUrl) && imageUrlNode != null)
            {
                video.ThumbUrl = imageUrlNode.InnerText.Trim();
            }

            // Quality and media url
            string mediaUrl = HttpUtility.HtmlDecode(mediaUrlNode.InnerText.Trim());
            if (Regex.Match(asset.OuterHtml, @"720p", RegexOptions.IgnoreCase | RegexOptions.Compiled).Success) 
            {
                video.Files[VideoQuality.HD720] = mediaUrl;
            } 
            else 
            {
                video.Files[VideoQuality.Unknown] = mediaUrl;
            }

            this.state = NodeState.Complete;

            return NodeResult.Success;
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
