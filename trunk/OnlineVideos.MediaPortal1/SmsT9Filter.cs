using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.MediaPortal1
{
    internal class SmsT9Filter
    {
        private Regex filter = null;
        private string numbers = String.Empty;

        private SortedDictionary<string, object> matches;

        public bool Matches(string name)
        {
            if (filter == null)
                return true;
            Match m = filter.Match(name);

            bool match = m.Success;
            while (m.Success)
            {
                string s = m.Captures[0].ToString().ToLower();
                if (!matches.ContainsKey(s))
                    matches.Add(s, null);
                m = m.NextMatch();
            }

            return match;
        }

        public void Add(char c)
        {
            string currentPattern = filter == null ? String.Empty : currentPattern = filter.ToString();

            switch (c)
            {
                case '1': currentPattern += "[1]"; numbers = numbers + c; break;
                case '2': currentPattern += "[2|a|b|c]"; numbers = numbers + c; break;
                case '3': currentPattern += "[3|d|e|f]"; numbers = numbers + c; break;
                case '4': currentPattern += "[4|g|h|i]"; numbers = numbers + c; break;
                case '5': currentPattern += "[5|j|k|l]"; numbers = numbers + c; break;
                case '6': currentPattern += "[6|m|n|o]"; numbers = numbers + c; break;
                case '7': currentPattern += "[7|p|q|r|s]"; numbers = numbers + c; break;
                case '8': currentPattern += "[8|t|u|v]"; numbers = numbers + c; break;
                case '9': currentPattern += "[9|w|x|y|z]"; numbers = numbers + c; break;
                case '0': currentPattern += "[0|\\s]"; numbers = numbers + c; break;
                case '\b': if (!String.IsNullOrEmpty(currentPattern))
                    {
                        numbers = numbers.Substring(0, numbers.Length - 1);
                        currentPattern = currentPattern.Substring(0, currentPattern.LastIndexOf('['));
                    }
                    break;
            }
            if (String.IsNullOrEmpty(currentPattern))
                filter = null;
            else
                filter = new Regex(currentPattern, RegexOptions.IgnoreCase);
        }

        public void Clear()
        {
            filter = null;
            numbers = String.Empty;
        }

        public bool IsEmpty()
        {
            return filter == null;
        }

        public void StartMatching()
        {
            matches = new SortedDictionary<string, object>();
        }

        public override string ToString()
        {
            string hlabel = numbers;
            if (filter != null)
            {
                hlabel = hlabel + " {";
                string m = String.Empty;
                foreach (string s in matches.Keys)
                    m = m + ',' + s;
                if (String.IsNullOrEmpty(m))
                    hlabel = hlabel + '}';
                else
                    hlabel = hlabel + m.Substring(1) + '}';
            }

            return hlabel;
        }
    }
}