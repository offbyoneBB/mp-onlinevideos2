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

namespace OnlineVideos.MediaPortal1
{
    public static class Translator
    {
        #region Private variables

        private static Dictionary<string, string> _translations;
        private static readonly string _path = string.Empty;
        private static readonly DateTimeFormatInfo _info;

        #endregion        

        #region Constructor

        static Translator()
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

            Log.Instance.Info("Using language " + Lang);

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
                    Log.Instance.Warn("Cannot find translation file {0}.  Falling back to English (US)", langPath);
                else
                {
                    Log.Instance.Error("Error in translation xml file: {0}. Falling back to English (US)", Lang);
                    Log.Instance.Error(e);
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
                        Log.Instance.Error("Error in Translation Engine");
                        Log.Instance.Error(ex);
                    }
            }

            Type TransType = typeof(Translation);
            FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fieldInfos)
            {
                if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
                    TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
                else
                    Log.Instance.Info("Translation not found for field: {0}.  Using hard-coded English default.", fi.Name);
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
            Log.Instance.Info("Translating skin");
            foreach (string name in Strings.Keys)
            {
                SetProperty("#OnlineVideos.Translation." + name + ".Label", Translator.Strings[name]);
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

    }
}