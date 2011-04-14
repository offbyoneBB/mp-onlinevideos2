using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.ITunes
{
    public static class Extensions
    {

        /// <summary>
        /// Returns a String.Object that represents the current VideoQuality value
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string ToTitleString(this VideoQuality self)
        {
            switch (self)
            {
                case VideoQuality.FullHD:
                    return "HD 1080p";
                case VideoQuality.HD720:
                    return "HD 720p";
                case VideoQuality.HD480:
                    return "HD 480p";
                default:
                    return self.ToString();
            }
        }

    }
}
