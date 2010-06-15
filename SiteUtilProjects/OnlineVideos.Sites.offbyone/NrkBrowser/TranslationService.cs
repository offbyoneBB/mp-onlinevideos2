using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using OnlineVideos;

namespace Vattenmelon.Nrk.Browser.Translation
{
    public class TranslationService : ITranslationService
    {        
        Dictionary<string, string> translatedStrings = new Dictionary<string, string>();

        /// <summary>
        /// This is the constructor actually used by the plugin. It gets the currently used language in mediaportal and loads the appropriate language
        /// </summary>
        public void Init()
        {
            string lang = OnlineVideos.Translation.Lang;
            Log.Info("NrkBrowser using language " + lang);
            loadTranslatedStringsFromResource(lang);
        }

        private void loadTranslatedStringsFromResource(string lang)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                using (Stream fs = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("OnlineVideos.Sites.NrkBrowser.{0}.xml", lang)))
                {
                    doc.Load(fs);
                }                
                addStringsToDictionary(doc);
            }
            catch (Exception)
            {                
                Log.Warn("NrkBrowser : Error with translation file {0}.xml Falling back to hardcoded english", lang);
            }
        }

        private void addStringsToDictionary(XmlDocument doc)
        {
            foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
            {
                if (stringEntry.NodeType == XmlNodeType.Element)
                {
                    translatedStrings.Add(stringEntry.Attributes.GetNamedItem("id").Value, stringEntry.InnerText);
                }
            }
        }

        /// <summary>
        /// Returns the translated string for the key stringKey
        /// </summary>
        /// <param name="stringKey">The key for the translated string.</param>
        /// <returns>The translated string</returns>
        public string Get(string stringKey)
        {
            return translatedStrings[stringKey];
        }
        /// <summary>
        /// Method that checks if the key is translated.
        /// </summary>
        /// <param name="stringKey">The stringKey to check</param>
        /// <returns>True if it is translated, false if not.S</returns>
        public bool Contains(string stringKey)
        {
            return translatedStrings.ContainsKey(stringKey);
        }
        /// <summary>
        /// Returns the numger of translated strings.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfTranslatedStrings()
        {
            return translatedStrings.Count;
        }
    }
}