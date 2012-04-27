using System;
using System.Collections.Generic;
using System.Text;
using Google.GData.Client;
using Google.GData.Extensions;

namespace Google.GData.YouTube {
    /// <summary>
    /// Entry API customization class for defining entries in a Show feed.
    /// </summary>
    public class ShowSeasonEntry : YouTubeBaseEntry {
        public ShowSeasonEntry()
            : base() {
            this.ProtocolMajor = VersionDefaults.VersionTwo;

            this.AddExtension(new FeedLink());
        }

        /// <summary>
        /// returns the gd:feedLink element for clips
        /// </summary>
        /// <returns></returns>
        public FeedLink ClipLink {
            get {
                List<FeedLink> list = FindExtensions<FeedLink>(GDataParserNameTable.XmlFeedLinkElement,
                    GDataParserNameTable.gNamespace);

                foreach (FeedLink fl in list) {
                    if (fl.Rel.EndsWith("#season.clips")) {
                        return fl;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// returns the gd:feedLink element for episodes
        /// </summary>
        /// <returns></returns>
        public FeedLink EpisodeLink {
            get {
                List<FeedLink> list = FindExtensions<FeedLink>(GDataParserNameTable.XmlFeedLinkElement,
                    GDataParserNameTable.gNamespace);

                foreach (FeedLink fl in list) {
                    if (fl.Rel.EndsWith("#season.episodes")) {
                        return fl;
                    }
                }
                return null;
            }
        }
    }
}
