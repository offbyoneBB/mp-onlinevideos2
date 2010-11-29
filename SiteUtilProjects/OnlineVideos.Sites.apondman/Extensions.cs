using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pondman.OnlineVideos;

namespace OnlineVideos.Sites.apondman
{
    /// <summary>
    /// Extension utility container for SiteUtils in this library
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Creates a comma seperated string using the elements of the collection
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string ToCommaSeperatedString(this List<string> self)
        {
            return self.Count > 0 ? self.ToString(", ") : " ";
        }

        /// <summary>
        /// Joins a string[] together with the the given seperator
        /// </summary>
        /// <param name="seperator"></param>
        /// <returns>string output</returns>
        public static string ToString(this List<string> self, string seperator)
        {
            return string.Join(seperator, self.ToArray());
        }

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
