using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OnlineVideos.Sites.Utils
{
    class SubtitleReader
    {
        static string srtFormat = "{0}\r\n{1}0 --> {2}0\r\n{3}\r\n\r\n";
        public static string TimedText2SRT(string TTAFTxt)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(TTAFTxt);
            string builder = "";
            int id = 1;
            foreach (XmlNode p in doc.GetElementsByTagName("p"))
            {
                if (p.Attributes["begin"] == null || p.Attributes["end"] == null)
                    continue;
                string startTime = convertTime(p.Attributes["begin"].Value);
                string endTime = convertTime(p.Attributes["end"].Value);
                string subtitle = getSubtitleTxt(p);
                builder += string.Format(srtFormat, id, startTime, endTime, subtitle);
                id++;
            }
            return builder;
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

        static string convertTime(string input)
        {
            input = input.Replace(".", ",");
            int index = input.IndexOf(",");
            if (index > -1)
            {
                int count = 4 - input.Length - index;
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
