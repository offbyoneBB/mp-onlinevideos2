using System;

namespace OnlineVideos
{
    public class VideoInfo
    {
        public string Title;
        public string Title2;
        public string Description = "";
        public string VideoUrl = "";
        public string ImageUrl = "";
        public string Length;
        public string Tags = "";
        public object Other;
        public string SiteID = "";
        public int VideoID;
        public VideoInfo()
        {
            VideoID = -1;
        }
        public override string ToString()
        {
            return string.Format("Title:{0}\nDesc:{1}\nVidUrl:{2}\nImgUrl:{3}\nLength:{4}\nTags:{5}", Title, Description, VideoUrl, ImageUrl, Length, Tags);
        }
    }    
}
