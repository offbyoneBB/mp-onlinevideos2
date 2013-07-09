using System;
using System.Text;

namespace OnlineVideos.Sites.Pondman.ITunes {

    public class Configuration {
        
        public string RootTitle {
            get
            {
                return this.rootTitle;
            }
            set
            {
                this.rootTitle = value;
            }
        } string rootTitle = "iTunes Movie Trailers";       

        public string BaseUri {
            get { return _baseUri; }
            set { _baseUri = value; }
        } string _baseUri = "http://trailers.apple.com";

        public string FeedsUri {
            get {
                if (_feedsUri.StartsWith("/"))
                    return _baseUri + _feedsUri;

                return _feedsUri;
            }
            set { _feedsUri = value; }
        } string _feedsUri = "/trailers/home/feeds/";

        public string SearchUri {
            get {
                if (_searchUri.StartsWith("/"))
                    return _baseUri + _searchUri;

                return _searchUri;
            }
            set { _searchUri = value; }
        } string _searchUri = "/trailers/home/scripts/quickfind.php?q=";

        public string XmlMovieDetailsUri {
            get {
                if (_movieUri.StartsWith("/"))
                    return _baseUri + _movieUri;

                return _movieUri;
            }
            set { _movieUri = value; }
        } string _movieUri = "/appletv/studios/";

        public string FeaturedJustAddedUri {
            get { return FeedsUri + "just_added.json"; }
        }

        public string FeaturedExclusiveUri {
            get { return FeedsUri + "exclusive.json"; }
        }

        public string FeaturedMostPopularUri {
            get { return FeedsUri + "most_pop.json"; }
        }

        public string FeaturedGenresUri {
            get { return FeedsUri + "genres.json"; }
        }

        public string FeaturedStudiosUri {
            get { return FeedsUri + "studios.json"; }
        }

        /// <summary>
        /// Adds the value of BaseUri when the uri is relative
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string FixUri(string uri)
        {
            if (uri.StartsWith("/"))
            {
                uri = BaseUri + uri;
            }

            return uri;
        }

    }
}
