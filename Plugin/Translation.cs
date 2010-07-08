using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Localisation;

namespace OnlineVideos
{
    public static class Translation
    {
        #region Private variables

        private static Dictionary<string, string> _translations;
        private static readonly string _path = string.Empty;
        private static readonly DateTimeFormatInfo _info;

        #endregion        

        #region Constructor

        static Translation()
        {
            try
            {
                Lang = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
                _info = DateTimeFormatInfo.GetInstance(CultureInfo.CurrentUICulture);
            }
            catch (Exception)
            {
                Lang = CultureInfo.CurrentUICulture.Name;
                _info = DateTimeFormatInfo.GetInstance(CultureInfo.CurrentUICulture);
            }

            Log.Info("Using language " + Lang);

            _path = Config.GetSubFolder(Config.Dir.Language, "OnlineVideos");

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            LoadTranslations();
        }

        public static void SetProperty(string property, string value)
        {
            if (property == null)
                return;

            //// If the value is empty always add a space
            //// otherwise the property will keep 
            //// displaying it's previous value
            if (String.IsNullOrEmpty(value))
                value = " ";

            GUIPropertyManager.SetProperty(property, value);
        }

        #endregion

        #region Public Properties

        // Gets the language actually used (after checking for localization file and fallback).
        public static string Lang { get; private set; }

        /// <summary>
        /// Gets the translated strings collection in the active language
        /// </summary>
        public static Dictionary<string, string> Strings
        {
            get
            {
                if (_translations == null)
                {
                    _translations = new Dictionary<string, string>();
                    Type transType = typeof(Translation);
                    FieldInfo[] fields = transType.GetFields(BindingFlags.Public | BindingFlags.Static);
                    foreach (FieldInfo field in fields)
                    {
                        _translations.Add(field.Name, field.GetValue(transType).ToString());
                    }
                }
                return _translations;
            }
        }

        #endregion        

        private static int LoadTranslations()
        {
            XmlDocument doc = new XmlDocument();
            Dictionary<string, string> TranslatedStrings = new Dictionary<string, string>();
            string langPath = "";
            try
            {
                langPath = Path.Combine(_path, Lang + ".xml");
                doc.Load(langPath);
            }
            catch (Exception e)
            {
                if (Lang == "en-US")
                    return 0; // otherwise we are in an endless loop!

                if (e.GetType() == typeof(FileNotFoundException))
                    Log.Warn("Cannot find translation file {0}.  Falling back to English (US)", langPath);
                else
                {
                    Log.Error("Error in translation xml file: {0}. Falling back to English (US)", Lang);
                    Log.Error(e);
                }

                Lang = "en-US";
                return LoadTranslations();
            }
            foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
            {
                if (stringEntry.NodeType == XmlNodeType.Element)
                    try
                    {
                        TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("Field").Value, stringEntry.InnerText);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in Translation Engine");
                        Log.Error(ex);
                    }
            }

            Type TransType = typeof(Translation);
            FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fieldInfos)
            {
                if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
                    TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
                else
                    Log.Info("Translation not found for field: {0}.  Using hard-coded English default.", fi.Name);
            }
            return TranslatedStrings.Count;
        }

        #region Public Methods

        public static string GetByName(string name)
        {
            if (!Strings.ContainsKey(name))
                return name;

            return Strings[name];
        }

        public static string GetByName(string name, params object[] args)
        {
            return String.Format(GetByName(name), args);
        }

        /// <summary>
        /// Takes an input string and replaces all ${named} variables with the proper translation if available
        /// </summary>
        /// <param name="input">a string containing ${named} variables that represent the translation keys</param>
        /// <returns>translated input string</returns>
        public static string ParseString(string input)
        {
            Regex replacements = new Regex(@"\$\{([^\}]+)\}");
            MatchCollection matches = replacements.Matches(input);
            foreach (Match match in matches)
            {
                input = input.Replace(match.Value, GetByName(match.Groups[1].Value));
            }
            return input;
        }


        public static void TranslateSkin()
        {
            Log.Info("Translating skin");
            foreach (string name in Translation.Strings.Keys)
            {
                SetProperty("#OnlineVideos.Translation." + name + ".Label", Translation.Strings[name]);
            }
        }

        //public static string GetMediaType(MediaType mediaType)
        //{
        //  switch (mediaType)
        //  {
        //    case MyAlarm.MediaType.File:
        //      return File;

        //    case MyAlarm.MediaType.PlayList:
        //      return Playlist;

        //    case MyAlarm.MediaType.Message:
        //      return Message;

        //    default:
        //      return String.Empty;
        //  }
        //}

        public static string GetDayName(DayOfWeek dayOfWeek)
        {
            return _info.GetDayName(dayOfWeek);
        }
        public static string GetShortestDayName(DayOfWeek dayOfWeek)
        {
            return _info.GetShortestDayName(dayOfWeek);
        }

        #endregion

        #region Translations / Strings

        /// <summary>
        /// These will be loaded with the language files content
        /// if the selected lang file is not found, it will first try to load en(us).xml as a backup
        /// if that also fails it will use the hardcoded strings as a last resort.
        /// </summary>

        // A
        public static string AddingToFavorites = "adding to favorites";
        public static string Airdate = "Aired";
        public static string AlreadyDownloading = "Already downloading this file.";
        public static string All = "All";
        public static string Actions = "Actions";
        public static string Actors = "Actors";
        public static string AddToMySites = "Add to my sites";
        public static string AddToFavourites = "Add to favourites";

        // B
        public static string Broken = "Broken";
        public static string Buffered = "buffered";

        // C
        public static string Category = "Category";
        public static string Categories = "Categories";
        public static string Creator = "Creator";

        // D
        public static string DateOfRelease = "Date of Release";
        public static string Directors = "Directors";
        public static string Download = "Download";
        public static string Downloading = "Downloading";
        public static string DownloadingDescription = "Shows a list of downloads currently running.";
        public static string DownloadFailed = "Download failed: {0}";
        public static string DownloadCancelled = "Download Cancelled";
        public static string DownloadComplete = "Download Complete";
        public static string DownloadedVideos = "Downloaded Videos";
        public static string Delete = "Delete";
        public static string Default = "Default";
        public static string Done = "Done";
        public static string DeletingOldThumbs = "Deleting old thumbnails";

        // E
        public static string Error = "Error";
        public static string EnterPin = "Enter Pin";

        // F
        public static string Favourites = "Favorites";
        public static string Filter = "Filter";
        public static string FullUpdate = "Full Update";

        // G
        public static string Genre = "Genre";
        public static string GettingVideoDetails = "getting video details";
        public static string GettingCategoryVideos = "getting category videos";
        public static string GettingFavoriteVideos = "getting favorite videos";
        public static string GettingSearchResults = "getting search results";
        public static string GettingRelatedVideos = "getting related videos";
        public static string GettingFilteredVideos = "getting filtered videos";
        public static string GettingNextPageVideos = "getting next page videos";
        public static string GettingPreviousPageVideos = "getting previous page videos";
        public static string GettingPlaybackUrlsForVideo = "getting playback urls for video";
        public static string GettingDynamicCategories = "getting dynamic categories";

        // H

        // I

        // L
        public static string Language = "Language";
        public static string LayoutList = "Layout: List";
        public static string LayoutIcons = "Layout: Icons";
        public static string LayoutBigIcons = "Layout: Big Icons";

        // M
        public static string ManageSites = "Manage Sites";
        public static string MaxResults = "Max Results";

        // N
        public static string Name = "Name";
        public static string NextPage = "Next page";
        public static string None = "None";
        public static string NoVideoFound = "No video found";
        public static string NewDllDownloaded = "New dll downloaded!";

        // O

        // P
        public static string PlayAll = "Play all";
        public static string PlotOutline = "Plot outline";
        public static string PreviousPage = "Previous page";
        public static string PerformAutomaticUpdate = "Perform automatic update?";

        // R
        public static string RemoveFromFavorites = "Remove from favorites";
        public static string RemovingFromFavorites = "removing from favorites";
        public static string Refresh = "Refresh";
        public static string RelatedVideos = "Related Videos";
        public static string Reported = "Reported";
        public static string Runtime = "Runtime";
        public static string RemoveFromMySites = "Remove from my sites";
        public static string RestartMediaPortal = "Restart MediaPortal!";
        public static string RetrievingRemoteDlls = "Retrieving remote dlls ...";
        public static string RetrievingRemoteSites = "Retrieving remote sites ...";

        // S
        public static string SearchResults = "Search results";
        public static string SelectSource = "Select source";
        public static string Sites = "Sites";
        public static string SortOptions = "Sort options";
        public static string Search = "Search";
        public static string SetDownloadFolderInConfig = "Please set a download folder in Configuration!";
        public static string State = "State";
        public static string SavingLocalSiteList = "Saving local site list ...";
        public static string ShowReports = "Show creator's and user's reports";
        public static string StartingPlayback = "starting playback";

        // T
        public static string Timeframe = "Timeframe";
        public static string Timeout = "Timeout";
        public static string Tags = "Tags";

        // U
        public static string Updated = "Updated";
        public static string Updatable = "Updatable";
        public static string UpdateMySite = "Update my site";
        public static string UpdateMySiteSkipCategories = "Update my site (skip categories)";
        public static string UpdateAllYourSites = "This will update all your current sites.";
        public static string UnableToPlayVideo = "Unable to play the video. No URL.";
        public static string UnableToDownloadVideo = "Unable to download the video. Invalid URL.";

        // V
        public static string Videos = "Videos";

        // W
        public static string Working = "Working";

        // Y

        #endregion

    }
}