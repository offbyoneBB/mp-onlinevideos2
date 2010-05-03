using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vattenmelon.Nrk.Domain;

namespace Vattenmelon.Nrk.Parser
{
    public class Search
    {
        private List<Item> searchHits;

        public List<Item> SearchHits
        {
            get { return searchHits; }
        }

        public Search(String htmlData)
        {
            string regexQuery = "<li>.*?<a href=\"(?<link>[^\"]*)\" id=\"ctl00.*?\" title=\"(?<type>[^:]*).*?\">(?<title>[^<]*)</a>.*?<p id=\"ctl00.*?\">(?<desc>[^<]*)</p>.*?</li>";

            Regex query = new Regex(regexQuery, RegexOptions.Singleline);
            MatchCollection result = query.Matches(htmlData);
            //Log.Info(string.Format("Matches found in search: {0}", result.Count));
            searchHits = Search.GetSearchHits(result);
        }

        public static List<Item> GetSearchHits(MatchCollection result)
        {
            List<Item> categories = new List<Item>();
            foreach (Match x in result)
            {
                addItemFromSearchHitToList(categories, x);
            }
            return categories;
        }

        private static void addItemFromSearchHitToList(List<Item> items, Match x)
        {
            String link = x.Groups["link"].Value;
            String type = x.Groups["type"].Value;
            String title = string.Format("{0}", x.Groups[3].Value);
            String description = x.Groups["desc"].Value;

            String id = link.Substring(x.Groups["link"].Value.LastIndexOf("/", x.Groups["link"].Value.Length) + 1);

            if (type.Equals("Videoindeks") || type.Equals("Audioindeks"))
            {
                Clip c = CreateIndexClip(id, title, x);
                items.Add(c);
            }
            else if (type.Equals("Video") || type.Equals("Audio"))
            {
                Clip c = CreateClip(id, title, x);
                items.Add(c);
            }
            else if (type.Equals("Program"))
            {
                Program p = new Program(id, title, description, "");
                items.Add(p);
            }
            else if (type.Equals("Folder"))
            {
                Folder f = new Folder(id, title);
                f.Description = x.Groups[4].Value;
                items.Add(f);
            }
            else
            {
                Console.WriteLine("feil: " + type);
                //Log.Error(NrkParserConstants.PLUGIN_NAME + ": unsupported type: " + x.Groups[2].Value);
            }
        }

        private static Clip CreateClip(string id, string title, Match x)
        {
            Clip c = new Clip(id, title);
            c.Description = x.Groups[4].Value;
            return c;
        }

        private static Clip CreateIndexClip(string id, string title, Match x)
        {
            Clip c = CreateClip(id, title, x);
            c.Type = Clip.KlippType.INDEX;
            return c;
        }
    }
}
