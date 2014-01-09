using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    public class TelegraafTVUtil : BrightCoveUtil
    {
        protected override List<VideoInfo> Parse(string url, string data)
        {
            int p = url.LastIndexOf('=');
            int currPage;
            nextPageAvailable = false;
            if (int.TryParse(url.Substring(p + 1), out currPage))
            {
                nextPageAvailable = currPage < 10;
                nextPageUrl = url.Substring(0, p + 1) + (currPage + 1).ToString();
            }

            return base.Parse(url, data);
        }
    }
}
