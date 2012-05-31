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
		private static readonly DateTimeFormatInfo _info;
        
		// Gets the language actually used (after checking for localization file and fallback).
        public static string Lang { get; private set; }        
        
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

            Log.Instance.Info("Using language '{0}'", Lang);

            string _path = Config.GetSubFolder(Config.Dir.Language, "OnlineVideos");

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

			Lang = TranslationLoader.LoadTranslations(Lang, _path);
        }

        public static void TranslateSkin()
        {
            Log.Instance.Debug("Translating skin");
			foreach (var nameTrans in Translation.Instance.Strings)
            {
				GUIPropertyManager.SetProperty("#OnlineVideos.Translation." + nameTrans.Key + ".Label", nameTrans.Value);
            }
        }

        public static string GetDayName(DayOfWeek dayOfWeek)
        {
            return _info.GetDayName(dayOfWeek);
        }

        public static string GetShortestDayName(DayOfWeek dayOfWeek)
        {
            return _info.GetShortestDayName(dayOfWeek);
        }
    }
}