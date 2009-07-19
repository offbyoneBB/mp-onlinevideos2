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
        public string StartTime = "";
        public object Other;
        
        /// <summary>This field is only used by the <see cref="FavoriteUtil"/> to store the Name of the Site where this Video came from.</summary>
        public string SiteName = "";
                
        public override string ToString()
        {
            return string.Format("Title:{0}\nDesc:{1}\nVidUrl:{2}\nImgUrl:{3}\nLength:{4}\nTags:{5}", Title, Description, VideoUrl, ImageUrl, Length, Tags);
        }

        public double GetSecondsFromStartTime()
        {
            // Example: startTime = 02:34:25.00 should result in 9265 seconds
            double hours = new double();
            double minutes = new double();
            double seconds = new double();

            double.TryParse(StartTime.Substring(0, 2), out hours);
            double.TryParse(StartTime.Substring(3, 2), out minutes);
            double.TryParse(StartTime.Substring(6, 2), out seconds);

            seconds += (((hours * 60) + minutes) * 60);

            return seconds;
        }
    }    
}
