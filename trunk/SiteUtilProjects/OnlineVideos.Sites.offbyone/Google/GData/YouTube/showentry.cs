using System;
using System.Collections.Generic;
using System.Text;
using Google.GData.Client;
using Google.GData.Extensions;

namespace Google.GData.YouTube {
    /// <summary>
    /// Entry API customization class for defining entries in a Show feed.
    /// </summary>
    public class ShowEntry : YouTubeBaseEntry {
        public ShowEntry()
            : base() {
            this.ProtocolMajor = VersionDefaults.VersionTwo;

            this.AddExtension(new MediaGroup());
            this.AddExtension(new FeedLink());
        }

        /// <summary>
        /// returns the gd:feedLink element
        /// </summary>
        /// <returns></returns>
        public FeedLink FeedLink {
            get {
                return FindExtension(GDataParserNameTable.XmlFeedLinkElement,
                    GDataParserNameTable.gNamespace) as FeedLink;
            }
            set {
                ReplaceExtension(GDataParserNameTable.XmlFeedLinkElement,
                    GDataParserNameTable.gNamespace,
                    value);
            }
        }

        /// <summary>
        /// returns the media:rss group container element
        /// </summary>
        public MediaGroup Media {
            get {
                return FindExtension(Google.GData.Extensions.MediaRss.MediaRssNameTable.MediaRssGroup,
                    Google.GData.Extensions.MediaRss.MediaRssNameTable.NSMediaRss) as MediaGroup;
            }
            set {
                ReplaceExtension(Google.GData.Extensions.MediaRss.MediaRssNameTable.MediaRssGroup,
                    Google.GData.Extensions.MediaRss.MediaRssNameTable.NSMediaRss,
                    value);
            }
        }
    }
}
