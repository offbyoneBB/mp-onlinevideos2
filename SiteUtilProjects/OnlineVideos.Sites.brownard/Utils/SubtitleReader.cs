using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites.Utils
{
    public class SubtitleReader
    {
        static string srtFormat = "{0}\r\n{1} --> {2}\r\n{3}\r\n\r\n";

        // Used to parse the start and end times in a vtt file which should look something like:
        // 00:00:57.960 --> 00:00:59.960 align:middle line:-3
        static readonly Regex VTT_TIME_REGEX = new Regex(@"([\d:\.,]+)\s*-->\s*([\d:\.,]+)");

        static readonly Regex TAG_REPLACE_REGEX = new Regex(@"<[^>]*>", RegexOptions.Singleline);

        public static string TimedText2SRT(string TTAFTxt)
        {
            if (string.IsNullOrEmpty(TTAFTxt))
                return null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(TTAFTxt);
            StringBuilder subtitleBuilder = new StringBuilder("");
            int id = 1;
            foreach (XmlNode p in doc.GetElementsByTagName("p"))
            {
                if (p.Attributes["begin"] == null || p.Attributes["end"] == null)
                    continue;
                string startTime = convertTTAFTime(p.Attributes["begin"].Value);
                string endTime = convertTTAFTime(p.Attributes["end"].Value);
                string subtitle = getSubtitleTxt(p);
                subtitleBuilder.AppendFormat(srtFormat, id, startTime, endTime, subtitle);
                id++;
            }
            return subtitleBuilder.ToString();
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
                    item.Subtitle += getSubtitleTxt(p);
                items.Add(item);
            }
            StringBuilder subtitleBuilder = new StringBuilder("");
            int id = 1;
            for(int x = 0; x < items.Count - 1; x++)
            {
                if (string.IsNullOrEmpty(items[x].Subtitle))
                    continue;
                System.TimeSpan ts = TimeSpan.FromMilliseconds(items[x].StartTime);
                string startTime = new DateTime(ts.Ticks).ToString("HH:mm:ss,fff");
                ts = TimeSpan.FromMilliseconds(items[x + 1].StartTime);
                string endTime = new DateTime(ts.Ticks).ToString("HH:mm:ss,fff");
                subtitleBuilder.AppendFormat(srtFormat, id, startTime, endTime, items[x].Subtitle);
                id++;
            }
            return subtitleBuilder.ToString();
        }

        public static string VTT2SRT(string vttTxt)
        {
            if (string.IsNullOrEmpty(vttTxt))
                return null;

            // A web-vtt file should start with the text 'WEBVTT' followed
            // by blocks of cues delimited by a preceeding blank line.
            // A cue block can optionally start with a title.
            // The character sequence '-->' is not allowed to appear anywhere
            // in the file except between the cue's start and end time.
            // The cue times don't have to include hours.
            // Comments blocks are also allowed and these are indicated by
            // a line starting with 'NOTE' and should be followed by a
            // blank line.
            // Web-vtt files may also contain css style sheets, we don't
            // support them and this parser is deliberately simple enough
            // to ignore them.

            // WEBVTT My vtt file
            //
            // 00:00:55.960 --> 00:00:57.960 align:middle line:-3
            // <c.cyan>Madre mia,</c>
            // <c.cyan>it's your language, not mine.</c>
            //
            // NOTE The following cue includes an optional title,
            // comments can also span multiple lines.
            //
            // My cue title
            // 00:00:57.960 --> 00:00:59.960 align:middle line:-3
            // <c.cyan>Madre mia,</c>
            // <c.cyan>it's your language, not mine.</c>

            // Basically all we do below is ignore everything until we find
            // a line containing '-->', parse the times, then strip out all
            // tags from the following lines, leaving just the line breaks
            // and text, until we reach a blank line indicating the end of the
            // current cue. Then repeat the process for the next cue.

            // Used to store a complete srt cue. If start time
            // is null then it's assumed that the start of the
            // next cue hasn't been found yet
            int id = 1;
            string startTime = null;
            string endTime = null;
            string subtitle = "";

            StringBuilder subtitleBuilder = new StringBuilder("");
            using (var sr = new StringReader(vttTxt))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // If it's a blank line then it's either then end of
                    // a block or we haven't found the first block yet.
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (startTime != null)
                        {
                            // If we have a start time then assume it's the end
                            // of a block and add it to our srt text.
                            subtitleBuilder.AppendFormat(srtFormat, id, startTime, endTime, subtitle);
                            // Reset and increment the current block.
                            startTime = null;
                            endTime = null;
                            subtitle = "";
                            id++;
                        }
                        continue;
                    }

                    if (startTime == null)
                    {
                        // If we haven't got a start time for this block then
                        // try and find start and end time in this line.
                        if (!line.Contains("-->"))
                            continue;

                        // This line should look something like this
                        // 00:00:57.960 --> 00:00:59.960 align:middle line:-3
                        Match match = VTT_TIME_REGEX.Match(line);
                        if (!match.Success)
                            continue;

                        TimeSpan start;
                        TimeSpan end;
                        if (!TimeSpan.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out start) ||
                            !TimeSpan.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out end))
                            continue;
                        startTime = start.ToString(@"hh\:mm\:ss\,fff"); //new DateTime(start.Ticks).ToString("HH:mm:ss,fff");
                        endTime = end.ToString(@"hh\:mm\:ss\,fff"); //new DateTime(end.Ticks).ToString("HH:mm:ss,fff");
                    }
                    else
                    {
                        // If we have a start time then this line must be part of
                        // the caption. Honour the new line and strip any tags.
                        if (subtitle != "")
                            subtitle += "\r\n";
                        subtitle += TAG_REPLACE_REGEX.Replace(line, "");
                    }
                }
            }

            // Cues are only added when a blank line is reached so we still
            // might need to add the last cue if it's last line is the last
            // in the file.
            if (startTime != null && !string.IsNullOrEmpty(subtitle))
                subtitleBuilder.AppendFormat(srtFormat, id, startTime, endTime, subtitle);

            return subtitleBuilder.ToString();
        }

        static string getSubtitleTxt(XmlNode p)
        {
            string text = "";
            foreach (XmlNode t in p.ChildNodes)
            {
                if (t.NodeType == XmlNodeType.Text)
                    text += t.Value;
                else if (t.Name == "br")
                    text += "\r\n";
                else if (t.Name == "span")
                    text += getSubtitleTxt(t);
            }
            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2);
            return text;
        }

        static string convertTTAFTime(string input)
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

    class SAMIItem
    {
        internal int StartTime { get; set; }
        internal string Subtitle { get; set; }
    }
}
