using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.ITunes.Nodes {
    public class Poster : ExternalContentNodeBase {

        public string Large {
            get {
                if (_large == null) {
                    string poster = Uri;
                    if (poster.EndsWith("poster.jpg")) {
                        poster = poster.Replace("poster.jpg", "poster-large.jpg");
                        Large = poster;
                    } 
                    else if (poster.Contains("_20")) {
                        poster = poster.Replace("_20", "_l20");
                        Large = poster;
                    }
                    
                }
                return _large;
            }
            set {
                _large = value;
            }
        } string _large;

        public string XL
        {
            get {
                if (_xlarge == null) {
                    string poster = Uri;
                    if (poster.EndsWith("poster.jpg")) {
                        poster = poster.Replace("poster.jpg", "poster-xlarge.jpg");
                        XL = poster;
                    }
                    else if (poster.Contains("_20")) {
                        poster = poster.Replace("_20", "_xl20");
                        XL = poster;
                    }
                }
                return _xlarge;
            }
            set {
                _xlarge = value;
            }
        } string _xlarge;

    }
}
