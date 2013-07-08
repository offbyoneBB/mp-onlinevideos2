using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.ITunes.Nodes {
    public class Poster : ExternalContentNodeBase {

        public string Large {
            get {
                if (_large == null) {
                    _large = Uri;
                    if (_large.EndsWith("poster.jpg"))
                    {
                        _large = _large.Replace("poster.jpg", "poster-large.jpg");
                    }
                    else if (_large.Contains("_20"))
                    {
                        _large = _large.Replace("_20", "_l20");
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
