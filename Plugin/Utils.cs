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
    }
}
