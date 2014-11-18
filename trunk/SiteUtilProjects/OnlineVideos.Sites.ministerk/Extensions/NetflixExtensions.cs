using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace OnlineVideos.Sites.Ministerk.Extensions
{
    static class NetflixExtensions
    {
        public static bool IsPlayNow(this Category c)
        {
            var other = c.Other;
            return (other is SerializableDictionary<string, string> && (other as SerializableDictionary<string, string>).ContainsKey(NetflixUtils.PlayNow));
        }

        public static void SetPlayNow(this  Category c, bool playNow)
        {
            SerializableDictionary<string, string> d;
            if (c.Other == null || !(c.Other is SerializableDictionary<string, string>))
                d = new SerializableDictionary<string, string>();
            else
                d = c.Other as SerializableDictionary<string, string>;
            if (d.ContainsKey(NetflixUtils.PlayNow))
                d.Remove(NetflixUtils.PlayNow);
            if (playNow)
                d.Add(NetflixUtils.PlayNow, NetflixUtils.PlayNow);
            c.Other = d;
        }

        public static bool RememberDiscoveredItems(this Category c)
        {
            var other = c.Other;
            if (other is SerializableDictionary<string, string> && (other as SerializableDictionary<string, string>).ContainsKey(NetflixUtils.RememberDiscoveredItems))
            {
                return (other as SerializableDictionary<string, string>)[NetflixUtils.RememberDiscoveredItems] == true.ToString();
            }
            return true;
        }

        public static void SetRememberDiscoveredItems(this  Category c, bool remember)
        {
            SerializableDictionary<string, string> d;
            if (c.Other == null || !(c.Other is SerializableDictionary<string, string>))
                d = new SerializableDictionary<string, string>();
            else
                d = c.Other as SerializableDictionary<string, string>;
            if (d.ContainsKey(NetflixUtils.RememberDiscoveredItems))
                d.Remove(NetflixUtils.RememberDiscoveredItems);
            d.Add(NetflixUtils.RememberDiscoveredItems, remember.ToString());
            c.Other = d;
        }

        public static void SetProfile(this  Category c, JToken profile)
        {
            SerializableDictionary<string, string> d;
            if (c.Other == null || !(c.Other is SerializableDictionary<string, string>))
                d = new SerializableDictionary<string, string>();
            else
                d = c.Other as SerializableDictionary<string, string>;
            if (d.ContainsKey(NetflixUtils.Profile))
                d.Remove(NetflixUtils.Profile);
            d.Add(NetflixUtils.Profile, profile.ToString());
            c.Other = d;
        }

        public static JObject GetProfile(this  Category c)
        {
            if (c.Other == null || !(c.Other is SerializableDictionary<string, string>))
                return null;
            SerializableDictionary<string, string> d = c.Other as SerializableDictionary<string, string>;
            if (!d.ContainsKey(NetflixUtils.Profile))
                return null;
            return (JObject)JsonConvert.DeserializeObject((string)d[NetflixUtils.Profile]);
        }

        private static string GetStringProperty(Category c, string key)
        {
            var other = c.Other;
            if (other != null && other is SerializableDictionary<string, string> && (other as SerializableDictionary<string, string>).ContainsKey(key))
                return (other as SerializableDictionary<string, string>)[key];
            return "";
        }

        private static void SetStringProperty(Category c, string s, string key)
        {
            SerializableDictionary<string, string> d;
            if (c.Other == null || !(c.Other is SerializableDictionary<string, string>))
                d = new SerializableDictionary<string, string>();
            else
                d = c.Other as SerializableDictionary<string, string>;

            if (d.ContainsKey(key))
                d.Remove(key);
            d.Add(key, s);
            c.Other = d;
        }

        public static void SetStartIndex(this Category c, string s)
        {
            NetflixExtensions.SetStringProperty(c, s, NetflixUtils.StartIndex);
        }

        public static string GetStartIndex(this Category c)
        {
            return NetflixExtensions.GetStringProperty(c, NetflixUtils.StartIndex);
        }

        public static void SetTrackId(this Category c, string s)
        {
            NetflixExtensions.SetStringProperty(c, s, NetflixUtils.TrackId);
        }

        public static string GetTrackId(this Category c)
        {
            return NetflixExtensions.GetStringProperty(c, NetflixUtils.TrackId);
        }

        public static void SetState(this Category c, string s)
        {
            NetflixExtensions.SetStringProperty(c, s, NetflixUtils.State);
        }

        public static string GetState(this Category c)
        {
            return NetflixExtensions.GetStringProperty(c, NetflixUtils.State);
        }

        public static void SetData(this Category c, string s)
        {
            NetflixExtensions.SetStringProperty(c, s, NetflixUtils.Data);
        }

        public static string GetData(this Category c)
        {
            return NetflixExtensions.GetStringProperty(c, NetflixUtils.Data);
        }

        public static bool IsProfilesState(this Category c)
        {
            return c.GetState() == NetflixUtils.ProfilesState;
        }

        public static bool IsProfileState(this Category c)
        {
            return c.GetState() == NetflixUtils.ProfileState;
        }

        public static bool IsKidsState(this Category c)
        {
            return c.GetState() == NetflixUtils.KidsState;
        }

        public static bool IsSinglePageCategoriesState(this Category c)
        {
            return c.GetState() == NetflixUtils.SinglePageCategoriesState;
        }

        public static bool IsMultiplePageCategoriesState(this Category c)
        {
            return c.GetState() == NetflixUtils.MultiplePageCategoriesState;
        }

        public static bool IsTitleState(this Category c)
        {
            return c.GetState() == NetflixUtils.TitleState;
        }

        public static bool IsEpisodesState(this Category c)
        {
            return c.GetState() == NetflixUtils.EpisodesState;
        }

        public static bool IsHelpState(this Category c)
        {
            return c.GetState() == NetflixUtils.HelpState;
        }

        public static bool IsHomeCategoriesState(this Category c)
        {
            return c.GetState() == NetflixUtils.HomeCategoriesState;
        }

        public static bool IsHomeCategoryState(this Category c)
        {
            return c.GetState() == NetflixUtils.HomeCategoryState;
        }
    }
}
