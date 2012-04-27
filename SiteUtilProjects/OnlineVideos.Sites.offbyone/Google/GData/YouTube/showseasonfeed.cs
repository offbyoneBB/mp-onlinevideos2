using System;
using System.Collections.Generic;
using System.Text;
using Google.GData.Client;

namespace Google.GData.YouTube {
    /// <summary>
    /// A user's shows feed contains a list of the shows created by
    /// that user.
    /// </summary>
    public class ShowSeasonFeed : YouTubeFeed {
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="uriBase">the base URI of the feedEntry</param>
        /// <param name="iService">the Service to use</param>
        public ShowSeasonFeed(Uri uriBase, IService iService)
            : base(uriBase, iService) {
        }

        /// <summary>
        /// this needs to get implemented by subclasses
        /// </summary>
        /// <returns>AtomEntry</returns>
        public override AtomEntry CreateFeedEntry() {
            return new ShowSeasonEntry();
        }
    }
}
