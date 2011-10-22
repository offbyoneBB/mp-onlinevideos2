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

        private static readonly string _path = string.Empty;
        private static readonly DateTimeFormatInfo _info;
		private static Dictionary<string, string> TranslatedStrings = new Dictionary<string, string>();

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

            Log.Instance.Info("Using language '{0}'", Lang);

            _path = Config.GetSubFolder(Config.Dir.Language, "OnlineVideos");

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            LoadTranslations();
        }

        #endregion

        #region Public Properties

        // Gets the language actually used (after checking for localization file and fallback).
        public static string Lang { get; private set; }

        #endregion        

        private static int LoadTranslations()
        {
            XmlDocument doc = new XmlDocument();
            
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
                    Log.Instance.Warn("Cannot find translation file '{0}'.  Falling back to English (US)", langPath);
                else
                {
                    Log.Instance.Error("Error in translation xml file: '{0}'. Falling back to English (US)", Lang);
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

			SetTranslationsToSingleton();
            return TranslatedStrings.Count;
        }

		internal static void SetTranslationsToSingleton()
		{
			Type TransType = typeof(Translation);
			FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo fi in fieldInfos)
			{
				if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
					fi.SetValue(Translation.Instance, TranslatedStrings[fi.Name]);
				//TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
				else
					Log.Instance.Info("Translation not found for field: '{0}'. Using hard-coded English default.", fi.Name);
			}
		}

        #region Public Methods

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

        #endregion

    }
}