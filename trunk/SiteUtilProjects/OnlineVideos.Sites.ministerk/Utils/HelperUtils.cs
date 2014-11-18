using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils
{
    static class NetflixUtils
    {
        #region Category States
        public static readonly string ProfilesState = "ProfilesState";
        public static readonly string ProfileState = "ProfileState";
        public static readonly string KidsState = "KidsState";
        public static readonly string SinglePageCategoriesState = "SinglePageCategoriesState";
        public static readonly string MultiplePageCategoriesState = "MultiplePageCategoriesState";
        public static readonly string TitleState = "TitleState";
        public static readonly string EpisodesState = "EpisodesState";
        public static readonly string HelpState = "HelpState";
        public static readonly string HomeCategoriesState = "HomeCategoriesState";
        public static readonly string HomeCategoryState = "HomeCategoryState";

        #endregion

        #region Keys
        public static readonly string State = "State";
        public static readonly string PlayNow = "PlayNow";
        public static readonly string Profile = "Profile";
        public static readonly string TrackId = "TrackId";
        public static readonly string StartIndex = "StartIndex";
        public static readonly string Data = "Data";
        public static readonly string RememberDiscoveredItems = "RememberDiscoveredItems";
        #endregion

    }

    class HelperUtils
    {
        public static string GetRandomChars(int amount)
        {
            var random = new Random();
            var sb = new System.Text.StringBuilder(amount);
            for (int i = 0; i < amount; i++) sb.Append(System.Text.Encoding.ASCII.GetString(new byte[] { (byte)random.Next(65, 90) }));
            return sb.ToString();
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
