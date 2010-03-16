using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

        public static IList<SiteSettings> SiteSettingsFromXml(string siteXml)
        {
            siteXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OnlineVideoSites xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Sites>
" + siteXml + @"
</Sites>
</OnlineVideoSites>";
            System.IO.StringReader sr = new System.IO.StringReader(siteXml);
            System.Xml.Serialization.XmlSerializer ser = OnlineVideoSettings.getInstance().XmlSerImp.GetSerializer(typeof(SerializableSettings));
            SerializableSettings s = (SerializableSettings)ser.Deserialize(sr);
            return s.Sites;            
        }
    }
}
