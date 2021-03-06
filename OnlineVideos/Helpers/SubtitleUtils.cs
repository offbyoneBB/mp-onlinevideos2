﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Helpers
{
    public static class SubtitleUtils
    {
        class SAMIItem
        {
            internal int StartTime { get; set; }
            internal string Subtitle { get; set; }
        }

        static string srtFormat = "{0}\r\n{1} --> {2}\r\n{3}\r\n\r\n";

        public static string TimedText2SRT(string TTAFTxt)
        {
            if (string.IsNullOrEmpty(TTAFTxt))
                return null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(TTAFTxt);
            string builder = "";
            int id = 1;
            foreach (XmlNode p in doc.GetElementsByTagName("p"))
            {
                if (p.Attributes["begin"] == null || p.Attributes["end"] == null)
                    continue;
                string startTime = ConvertTTAFTime(p.Attributes["begin"].Value);
                string endTime = ConvertTTAFTime(p.Attributes["end"].Value);
                string subtitle = GetSubtitleTxt(p);
                builder += string.Format(srtFormat, id, startTime, endTime, subtitle);
                id++;
            }
            return builder;
        }

        public static string Webvtt2SRT(String webvttContent)
        {
            StringBuilder sb = new StringBuilder();

            using (StringReader sr = new StringReader(webvttContent))
            {
                string line;
                bool lastIsEmpty = true;
                int cueNumber = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith("WEBVTT"))
                    {
                        if (line.Contains("-->"))
                        {
                            if (lastIsEmpty)
                                sb.AppendLine((++cueNumber).ToString());
                            line = Regex.Replace(line, @"(\d{2}:\d{2}:\d{2})\.(\d{3}\s+)-->(\s+\d{2}:\d{2}:\d{2})\.(\d{3}\s*)", "$1,$2-->$3,$4");// Writes '00:00:19,620' instead of '00:00:19.620'
                        }
                        else
                            line = Regex.Replace(line, @"< *br */*>", "\r\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        sb.AppendLine(line);
                        lastIsEmpty = String.IsNullOrEmpty(line);
                    }
                }
            }

            return sb.ToString();
        }

        public static string SAMI2SRT(string SAMITxt)
        {
            if (string.IsNullOrEmpty(SAMITxt))
                return null;
            Match m = new Regex("<Body[^>]*>.*?</Body>", RegexOptions.Singleline).Match(SAMITxt);
            if (!m.Success)
                return null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(m.Value.Replace("<br>", "<br />"));
            List<SAMIItem> items = new List<SAMIItem>();
            XmlNodeList syncs = doc.SelectNodes("//Sync");
            foreach (XmlNode sync in syncs)
            {
                int start;
                if (sync.Attributes["Start"] == null || !int.TryParse(sync.Attributes["Start"].Value, out start))
                    continue;
                SAMIItem item = new SAMIItem();
                item.StartTime = start;
                foreach (XmlNode p in sync.SelectNodes("./P"))
                    item.Subtitle += GetSubtitleTxt(p);
                items.Add(item);
            }
            string subtitle = "";
            int id = 1;
            for (int x = 0; x < items.Count - 1; x++)
            {
                if (string.IsNullOrEmpty(items[x].Subtitle))
                    continue;
                System.TimeSpan ts = TimeSpan.FromMilliseconds(items[x].StartTime);
                string startTime = new DateTime(ts.Ticks).ToString("HH:mm:ss,fff");
                ts = TimeSpan.FromMilliseconds(items[x + 1].StartTime);
                string endTime = new DateTime(ts.Ticks).ToString("HH:mm:ss,fff");
                subtitle += string.Format(srtFormat, id, startTime, endTime, items[x].Subtitle);
                id++;
            }
            return subtitle;
        }

        static string GetSubtitleTxt(XmlNode p)
        {
            string text = "";
            foreach (XmlNode t in p.ChildNodes)
            {
                if (t.NodeType == XmlNodeType.Text)
                    text += t.Value;
                else if (t.Name == "br")
                    text += "\r\n";
                else if (t.Name == "span")
                    text += GetSubtitleTxt(t);
            }
            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2);
            return text;
        }

        static string ConvertTTAFTime(string input)
        {
            int index = input.LastIndexOf(".");
            if (index > -1)
                input = input.Replace(".", ",");
            else
            {
                index = input.LastIndexOf(":");
                if (index < 0)
                    return input;
                input = input.Remove(index) + "," + input.Substring(index + 1);
            }
            if (index > -1)
            {
                int count = 4 - (input.Length - index);
                if (count > 0)
                {
                    for (int x = 0; x < count; x++)
                        input += "0";
                }
            }
            return input;
        }
    }
}
