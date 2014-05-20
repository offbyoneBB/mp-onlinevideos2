using System;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for tvoparents.tvo.org.
    /// </summary>
    public class TVOParentsUtil : TVOUtil
    {
        protected override string baseUrlPrefix { get { return @"http://tvoparents.tvo.org"; } }
        protected override string mainCategoriesUrl { get { return @"http://tvoparents.tvo.org/video"; } }
        protected override string quickTab { get { return @"qt_9"; } }
    }
}
