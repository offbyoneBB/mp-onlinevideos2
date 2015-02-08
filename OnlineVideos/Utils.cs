using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos
{
    public static class Utils
    {
        public static string ToFriendlyCase(string PascalString)
        {
            return Regex.Replace(PascalString, "(?!^)([A-Z])", " $1");
        }

        public static string ReplaceEscapedUnicodeCharacter(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                @"(?:\\|%)[uU]([0-9A-Fa-f]{4})",
                delegate(System.Text.RegularExpressions.Match match)
                {
                    return ((char)Int32.Parse(match.Value.Substring(2), System.Globalization.NumberStyles.HexNumber)).ToString();
                });
        }

        public static DateTime UNIXTimeToDateTime(double unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime).ToLocalTime();
        }

        /// <summary>
        /// Example: time = 02:34:25.00 should result in 9265 seconds
        /// </summary>
        /// <returns></returns>
        public static double SecondsFromTimeString(string time)
        {
            try
            {
                double hours = 0.0d;
                double minutes = 0.0d;
                double seconds = 0.0d;

                double.TryParse(time.Substring(0, 2), out hours);
                double.TryParse(time.Substring(3, 2), out minutes);
                double.TryParse(time.Substring(6, 2), out seconds);

                seconds += (((hours * 60) + minutes) * 60);

                return seconds;
            }
            catch (Exception ex)
            {
                Log.Warn("Error getting seconds from StartTime ({0}): {1}", time, ex.Message);
                return 0.0d;
            }
        }

        /// <summary>
        /// Parse a string and return a canonical representation if it represented a time.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static string FormatDuration(string duration)
        {
            if (!string.IsNullOrEmpty(duration))
            {
                double seconds;
                if (double.TryParse(duration, System.Globalization.NumberStyles.None | System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"), out seconds))
                {
                    return new DateTime(TimeSpan.FromSeconds(seconds).Ticks).ToString("HH:mm:ss");
                }
                else return duration;
            }
            return "";
        }

        public static void SiteSettingsToXml(SerializableSettings sites, Stream stream)
        {
			var ctx = new System.Runtime.Serialization.StreamingContext();
			foreach (var site in sites.Sites)
			{
				site.OnSerializingMethod(ctx);
				CallOnSerializingRecursive(site.Categories, ctx);
			}
			var ser = new System.Xml.Serialization.XmlSerializer(typeof(SerializableSettings));
			ser.Serialize(XmlWriter.Create(stream, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true }), sites);
        }

        public static IList<SiteSettings> SiteSettingsFromXml(string siteXml)
        {
            siteXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OnlineVideoSites xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Sites>
" + siteXml + @"
</Sites>
</OnlineVideoSites>";
			return CrossDomain.OnlineVideosAppDomain.PluginLoader.CreateSiteSettingsFromXml(siteXml);
        }

		private static void CallOnDeserializedRecursive(IList<Category> cats, System.Runtime.Serialization.StreamingContext ctx)
		{
			if (cats != null)
			{
				foreach (var cat in cats)
				{
					cat.OnDeserializedMethod(ctx);
					CallOnDeserializedRecursive(cat.SubCategories, ctx);
				}
			}
		}
		private static void CallOnSerializingRecursive(IList<Category> cats, System.Runtime.Serialization.StreamingContext ctx)
		{
			if (cats != null)
			{
				foreach (var cat in cats)
				{
					cat.OnSerializingMethod(ctx);
					CallOnSerializingRecursive(cat.SubCategories, ctx);
				}
			}
		}
        public static IList<SiteSettings> SiteSettingsFromXml(TextReader reader)
        {
			var ser = new System.Xml.Serialization.XmlSerializer(typeof(SerializableSettings));
			SerializableSettings s = ser.Deserialize(reader) as SerializableSettings;
			if (s != null)
			{
				var ctx = new System.Runtime.Serialization.StreamingContext();
				foreach (var site in s.Sites) CallOnDeserializedRecursive(site.Categories, ctx);
				return s.Sites;
			}
			else
			{
				return null;
			}
        }

        public static string PlainTextFromHtml(string input)
        {
            string result = input;
            if (!string.IsNullOrEmpty(result))
            {
                // decode HTML escape character
                result = System.Web.HttpUtility.HtmlDecode(result);

                // Replace &nbsp; with space
                result = Regex.Replace(result, @"&nbsp;", " ", RegexOptions.Multiline);

                // Remove double spaces
                result = Regex.Replace(result, @"  +", "", RegexOptions.Multiline);

                // Replace <br/> with \n
                result = Regex.Replace(result, @"< *br */*>", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

                // Remove remaining HTML tags                
                result = Regex.Replace(result, @"<[^>]*>", "", RegexOptions.Multiline);

                // Replace multiple newlines with just one
                result = Regex.Replace(result, @"(\r?\n)+", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

                // Remove whitespace at the beginning and end
                result = result.Trim();
            }
            return result;
        }

        public static string[] Tokenize(string text, bool dropToken, params string[] tokens)
        {
            if( tokens.Length > 0){

                string regex = @"([";
                foreach (string s in tokens)
                    regex += s;
                regex += "])";
                Regex RE = new Regex(regex);
                if (dropToken)
                {
                    string output = RE.Replace(text, " ");
                    return (new Regex(@"\s").Split(output)); 
                }
                else
                    return (RE.Split(text));
            }
            return null;
        }

        /// <summary>
        /// Method to change the AllowUnsafeHeaderParsing property of HttpWebRequest.
        /// </summary>
        /// <param name="setState"></param>
        /// <returns></returns>
        public static bool SetAllowUnsafeHeaderParsing(bool setState)
        {
            try
            {
                //Get the assembly that contains the internal class
                Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
                if (aNetAssembly == null)
                    return false;

                //Use the assembly in order to get the internal type for the internal class
                Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (aSettingsType == null)
                    return false;

                //Use the internal static property to get an instance of the internal settings class.
                //If the static instance isn't created allready the property will create it for us.
                object anInstance = aSettingsType.InvokeMember("Section",
                                                                BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic,
                                                                null, null, new object[] { });
                if (anInstance == null)
                    return false;

                //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                if (aUseUnsafeHeaderParsing == null)
                    return false;

                // and finally set our setting
                aUseUnsafeHeaderParsing.SetValue(anInstance, setState);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Unsafe header parsing setting change failed: " + ex.ToString());
                return false;
            }
        }

        public static string DictionaryToString(Dictionary<string, string> dic)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, OmitXmlDeclaration = true };
            using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("dictionary");
                foreach (string key in dic.Keys)
                {
                    writer.WriteStartElement("item");
                    writer.WriteStartElement("key");
                    writer.WriteCData(key);
                    writer.WriteEndElement();
                    writer.WriteStartElement("value");
                    writer.WriteCData(dic[key]);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.Flush();
                writer.Close();
            }
            return sb.ToString();
        }

        public static Dictionary<string, string> DictionaryFromString(string input)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader(input)))
            {
                bool wasEmpty = reader.IsEmptyElement;
                reader.Read();
                if (wasEmpty) return null;
                reader.ReadStartElement("dictionary");
                while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("item");
                    reader.ReadStartElement("key");
                    string key = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    reader.ReadStartElement("value");
                    string value = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    dic.Add(key, value);
                    reader.ReadEndElement();
                    reader.MoveToContent();
                }
                reader.ReadEndElement();
            }
            return dic;
        }

        public static string GetSaveFilename(string input)
        {
            string safe = input;
            foreach (char lDisallowed in System.IO.Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(lDisallowed.ToString(), "");
            }
            foreach (char lDisallowed in System.IO.Path.GetInvalidPathChars())
            {
                safe = safe.Replace(lDisallowed.ToString(), "");
            }
            return safe;
        }

        public static string EncryptLine(string strLine)
        {
            if (string.IsNullOrEmpty(strLine)) return string.Empty;
            Ionic.Zlib.CRC32 crc = new Ionic.Zlib.CRC32();
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
            writer.Write(strLine);
            stream.Position = 0;
            return string.Format("{0}", crc.GetCrc32(stream));
        }

        public static string GetThumbFile(string url)
        {
            // gets a CRC code for the given url and returns a full file path to the image: thums_dir\crc.jpg|gif|png
            string possibleExtension = System.IO.Path.GetExtension(url).ToLower();
            if (possibleExtension != ".gif" & possibleExtension != ".jpg" & possibleExtension != ".png") possibleExtension = ".jpg";
            string name = string.Format("Thumbs{0}L{1}", EncryptLine(url), possibleExtension);
			return Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Cache\"), name);
        }

        /// <summary>
        /// find and set all configuration fields that are not default
        /// </summary>
        /// <param name="siteUtil"></param>
        /// <param name="siteSettings"></param>
        public static void AddConfigurationValues(Sites.SiteUtilBase siteUtil, SiteSettings siteSettings)
        {
            // 1. build a list of all the Fields that are used for OnlineVideosConfiguration
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            foreach (FieldInfo field in siteUtil.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false);
                if (attrs.Length > 0 && ((System.ComponentModel.CategoryAttribute)attrs[0]).Category == "OnlineVideosConfiguration")
                {
                    fieldInfos.Add(field);
                }
            }

            // 2. get a "clean" site by creating it with empty SiteSettings
            siteSettings.Configuration = new StringHash();
            Sites.SiteUtilBase cleanSiteUtil = Sites.SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings);

            // 3. compare and collect different settings
            foreach (FieldInfo field in fieldInfos)
            {
                object defaultValue = field.GetValue(cleanSiteUtil);
                object newValue = field.GetValue(siteUtil);
                if (defaultValue != newValue)
                {
                    // seems that if default value = false, and newvalue = false defaultvalue != newvalue returns true
                    // so added extra check
                    if (defaultValue == null || !defaultValue.Equals(newValue))
                        siteSettings.Configuration.Add(field.Name, newValue.ToString());
                }
            }
        }

        public static bool IsValidUri(string url)
        {
            Uri temp = null;
            return Uri.TryCreate(url, UriKind.Absolute, out temp);
        }

		/// <summary>
		/// Remove all items from a List that are not a valid Url
		/// </summary>
		/// <param name="urls"></param>
		public static void RemoveInvalidUrls(List<string> urls)
		{
			if (urls != null)
			{
				int i = 0;
				while (i < urls.Count)
				{
					if (string.IsNullOrEmpty(urls[i]) ||
						!Utils.IsValidUri((urls[i].IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator) > 0) ? urls[i].Substring(0, urls[i].IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator)) : urls[i]))
					{
						Log.Debug("Removed invalid url: '{0}'", urls[i]);
						urls.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
			}
		}

        public static string GetNextFileName(string fullFileName)
        {
            if (string.IsNullOrEmpty(fullFileName)) throw new ArgumentNullException("fullFileName");
            if (!File.Exists(fullFileName)) return fullFileName;

            string baseFileName = Path.GetFileNameWithoutExtension(fullFileName);
            string ext = Path.GetExtension(fullFileName);

            string filePath = Path.GetDirectoryName(fullFileName);
            var numbersUsed = Directory.GetFiles(filePath, baseFileName + "_(*)" + ext)
                    .Select(x => Path.GetFileNameWithoutExtension(x).Substring(baseFileName.Length+1))
                    .Select(x =>
                    {
                        int result;
                        return Int32.TryParse(x.Trim(new char[] { '(', ')' }), out result) ? result : 0;
                    })
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            var firstGap = numbersUsed
                    .Select((x, i) => new { Index = i, Item = x })
                    .FirstOrDefault(x => x.Index != x.Item);
            int numberToUse = firstGap != null ? firstGap.Item : numbersUsed.Count;
            return Path.Combine(filePath, baseFileName) + "_(" + numberToUse + ")" + ext;
        }

        public static T DeepCopy<T>(object objectToCopy)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, objectToCopy);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }

        public static string GetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5Hasher;
            byte[] data;
            int count;
            StringBuilder result;

            md5Hasher = System.Security.Cryptography.MD5.Create();
            data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Loop through each byte of the hashed data and format each one as a hexadecimal string.
            result = new StringBuilder();
            for (count = 0; count < data.Length; count++)
            {
                result.Append(data[count].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

		/// <summary>
		/// Workaround for .net issue documented here: https://connect.microsoft.com/VisualStudio/feedback/details/386695/system-uri-incorrectly-strips-trailing-dots
		/// </summary>
		/// <remarks>
		/// Workaround sets static flag in the UriParser and will affect all System.Uri classes created afterwards - APPLICATION wise (not just OnlineVideos)
		/// </remarks>
        public static void FixUriTrailingDots()
        {
            MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (getSyntax != null && flagsField != null)
            {
                foreach (string scheme in new[] { "http", "https" })
                {
                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
                    if (parser != null)
                    {
                        int flagsValue = (int)flagsField.GetValue(parser);
                        // Clear the CanonicalizeAsFilePath attribute
                        if ((flagsValue & 0x1000000) != 0)
                        {
                            flagsField.SetValue(parser, flagsValue & ~0x1000000);
                        }
                    }
                }
            }
        }

		public static void Randomize<T>(this List<T> list)
		{
			Random rng = new Random();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

        public static string GetRandomLetters(int amount)
        {
            var random = new Random();
            var sb = new StringBuilder(amount);
            for (int i = 0; i < amount; i++) sb.Append(Encoding.ASCII.GetString(new byte[] { (byte)random.Next('A', 'Z') }));
            return sb.ToString();
        }

        public static List<String> ParseASX(string data)
        {
            string asxData = data.ToLower();
            MatchCollection videoUrls = Regex.Matches(asxData, @"<ref\s+href\s*=\s*\""(?<url>[^\""]*)");
            List<String> urlList = new List<String>();
            foreach (Match videoUrl in videoUrls)
            {
                urlList.Add(videoUrl.Groups["url"].Value);
            }
            return urlList;
        }

        public static string ParseASX(string data, out string startTime)
        {
            startTime = "";
            string asxData = data.ToLower();
            XmlDocument asxDoc = new XmlDocument();
            asxDoc.LoadXml(asxData);
            XmlElement entryElement = asxDoc.SelectSingleNode("//entry") as XmlElement;
            if (entryElement == null) return "";
            XmlElement refElement = entryElement.SelectSingleNode("ref") as XmlElement;
            if (entryElement == null) return "";
            XmlElement startElement = entryElement.SelectSingleNode("starttime") as XmlElement;
            if (startElement != null) startTime = startElement.GetAttribute("value");
            return refElement.GetAttribute("href");
        }

		internal static DateTime RetrieveLinkerTimestamp(string filePath)
		{
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;
			byte[] b = new byte[2048];
			System.IO.Stream s = null;

			try
			{
				s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				s.Read(b, 0, 2048);
			}
			finally
			{
				if (s != null)
				{
					s.Close();
				}
			}

			int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
			int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
			dt = dt.AddSeconds(secondsSince1970);
			dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
			return dt;
		}
    }
}
