/*
 * Copyright (c) 2008 Terje Wiesener <wiesener@samfundet.no>
 * 
 * Loosely based on an anonymous (and slightly outdated) NRK parser in python for Myth-tv, 
 * please email me if you are the author :)
 * 
 * 2008-2009-2010 by Vattenmelon
 * 
 * */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.RegularExpressions;
using Vattenmelon.Nrk.Parser;
using Vattenmelon.Nrk.Domain;
using Vattenmelon.Nrk.Parser.Http;
using Vattenmelon.Nrk.Parser.Xml;
using Log = OnlineVideos.Log;

namespace Vattenmelon.Nrk.Parser
{
    public class NrkParser
    {        
        private IHttpClient httpClient;
       
        private static string BASE_URL = "http://www1.nrk.no/";
        private static string MAIN_URL = BASE_URL + "nett-tv/";
        private static string CATEGORY_URL = MAIN_URL + "tema/";
        private static string PROGRAM_URL = MAIN_URL + "prosjekt/";
        private static string DIREKTE_URL = MAIN_URL + "direkte/";
        private static string FOLDER_URL = MAIN_URL + "kategori/";
        private static string INDEX_URL = MAIN_URL + "indeks/";
        private static string STORY_URL = MAIN_URL + "spilleliste.ashx?story=";
        private static string SOK_URL_BEFORE = MAIN_URL + "DynamiskLaster.aspx?SearchResultList$search:{0}|sort:dato|page:{1}";
        private static string MEST_SETTE_URL = MAIN_URL + "ml/topp12.aspx?dager={0}&_=";
        private static string GET_COOKIE_URL = MAIN_URL + "hastighet.aspx?hastighet={0}&retururl=http://www1.nrk.no/nett-tv/";


        private CookieContainer cookieContainer;
        private int speed;

        public NrkParser(int speed)
        {
            cookieContainer = new CookieContainer();
            httpClient = new HttpClient(cookieContainer);
            this.Speed = speed;
        }

        public IHttpClient HttpClient
        {
            set { httpClient = value; }
        }

        public int Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                FetchUrl(String.Format(GET_COOKIE_URL, speed));
            }
        }

        public IList<Item> GetSearchHits(String keyword, int page)
        {
            String urlToFetch = String.Format(SOK_URL_BEFORE, keyword, page + 1);
            string data = FetchUrl(urlToFetch);
            Search search = new Search(data);
            return search.SearchHits;
        }        

        public IList<Item> GetCategories()
        {
            List<Item> categories = new List<Item>();
            string data = FetchUrl(MAIN_URL);
            Regex query = new Regex("<a href=\"/nett-tv/tema/(?<id>\\w*).*?>(?<kategori>[^<]*)</a>");
            MatchCollection result = query.Matches(data);

            foreach (Match x in result)
            {
              categories.Add(new Category(x.Groups["id"].Value, x.Groups["kategori"].Value));
            }
            return categories;
        }

        public IList<Item> GetSistePaaForsiden()
        {
            Log.Info(NrkParserConstants.LIBRARY_NAME + ": GetSistePaaForsiden()");
            string data;
            data = FetchUrl(MAIN_URL);
            Regex query =
                new Regex(
                    "<li><div><a href=\"/nett-tv/klipp/(?<id>[^\"]*)\" title=\"[^\"]*\"><img src=\"(?<imgsrc>[^\"]*)\" alt=\".*?\" width=\"100\" height=\"57\" /></a><h3><a href=\".*?\" title=\"(?<desc>[^\"]*)\">(?<title>[^<]*)</a></h3></div></li>",
                    RegexOptions.Compiled | RegexOptions.Singleline);
            MatchCollection matches = query.Matches(data);
            List<Item> clips = new List<Item>();
            Log.Info(NrkParserConstants.LIBRARY_NAME + ": Matches {0}", matches.Count);
            foreach (Match x in matches)
            {
                Clip c = new Clip(x.Groups["id"].Value, x.Groups["title"].Value);
                //c.TilhoerendeProsjekt = Int32.Parse(x.Groups["prosjekt"].Value);
                c.Description = x.Groups["desc"].Value;
                c.Bilde = x.Groups["imgsrc"].Value;
                clips.Add(c);
            }

            return clips;
        }

        public IList<Item> GetMestSette(int dager)
        {
            Log.Info(string.Format("{0}: GetMestSette(), siste {1} dager", NrkParserConstants.LIBRARY_NAME, dager));
            string data;
            String url = String.Format(MEST_SETTE_URL, dager);
            data = FetchUrl(url);
            Regex query =
                new Regex(
                    "<li.*?><div><a href=\".*?/nett-tv/.*?/.*?\" title=\".*?\"><img src=\"(?<imgsrc>[^\"]*)\" .*? /></a><h3><a href=\".*?/nett-tv/(?<type>[^/]*)/(?<id>[^\"]*)\" title=\".*?\">(?<title>[^<]*)</a></h3><div class=\"summary\"><p>Vist (?<antallvist>[^ganger]*) ganger.</p></div></div></li>",
                    RegexOptions.Singleline);
            MatchCollection matches = query.Matches(data);
            List<Item> clips = new List<Item>();

            Log.Info(NrkParserConstants.LIBRARY_NAME + ": Matches {0}", matches.Count);
            foreach (Match x in matches)
            {
                Clip c = new Clip(x.Groups["id"].Value, x.Groups["title"].Value);
                c.AntallGangerVist = x.Groups["antallvist"].Value;
                c.Description = c.AntallGangerVist;
                c.Bilde = x.Groups["imgsrc"].Value;
                if (x.Groups["type"].Value.Trim().ToLower().Equals("indeks"))
                {
                    c.Type = Clip.KlippType.INDEX;
                }
                clips.Add(c);
            }

            return clips;
        }

        public IList<Item> GetTopTabRSS(string site)
        {
            XmlRSSParser parser = new XmlRSSParser(NrkParserConstants.RSS_URL, site);
            return parser.getClips();
        }

        /**
         * Metode som sjekker om clippet er en vignett. Vi vil ikke ha disse inn i lista.
         */

        public static bool isNotShortVignett(Clip loRssItem)
        {
            return
                loRssItem.ID != NrkParserConstants.VIGNETT_ID_NATURE && loRssItem.ID != NrkParserConstants.VIGNETT_ID_SUPER && loRssItem.ID != NrkParserConstants.VIGNETT_ID_NYHETER &&
                loRssItem.ID != NrkParserConstants.VIGNETT_ID_SPORT;
        }

        public IList<Item> GetDirektePage()
        {
            String tab = "direkte";
            Log.Info(NrkParserConstants.LIBRARY_NAME + ": GetDirektePage(String)", tab);
            string data;
            data = FetchUrl(MAIN_URL + tab + "/");
            Regex query =
                new Regex(
                    "<li><div><a href=\"/nett-tv/direkte/(.*?)\" title=\"(.*?)\"><img src=\"(.*?)\" .*?/></a><h3><a href=\".*?\" title=\".*?\">.*?</a></h3></div></li>",
                    RegexOptions.Singleline);
            
            MatchCollection matches = query.Matches(data);
            List<Item> clips = new List<Item>();
            Log.Info(NrkParserConstants.LIBRARY_NAME + ": Matches {0}", matches.Count);
            foreach (Match x in matches)
            {
                Clip c = new Clip(x.Groups[1].Value, x.Groups[2].Value);
                String bildeUrl = x.Groups[3].Value;
                c.Bilde = bildeUrl;
                c.Type = Clip.KlippType.DIREKTE;
                c.VerdiLink = tab;
                clips.Add(c);
            }

            return clips;
        }

        public IList<Item> GetPrograms(Category category)
        {
            string data = FetchUrl(CATEGORY_URL + category.ID);
            Regex queryBilder =
                new Regex("<li><div><a href=\"/nett-tv/prosjekt/.*?\" title=\".*?\"><img src=\"(?<imgsrc>[^\"]*)\".*?/></a><h3><a href=\"/nett-tv/prosjekt/(?<id>[^\"]*)\" title=\".*?\">(?<title>[^<]*)</a></h3><div class=\"summary\"><p>(?<desc>[^<]*)</p></div></div></li>");
            MatchCollection matches = queryBilder.Matches(data);
            List<string> bilder = new List<string>();
            List<Item> programs = new List<Item>();
            foreach (Match x in matches)
            {
                programs.Add(new Program(x.Groups["id"].Value, x.Groups["title"].Value, x.Groups["desc"].Value, x.Groups["imgsrc"].Value));
            }

            return programs;
        }

        public IList<Item> GetAllPrograms() //all programs
        {
            string data = FetchUrl(MAIN_URL + "bokstav/@");
            Regex query =
                new Regex(
                    "<li><div><a href=\"/nett-tv/prosjekt/(.*?)\" title=\"(.*?)\"><img src=\"(.*?)\".*?></a><h3><a href=\".*?\" title=\".*?\">.*?</a></h3><div class=\"summary\"><p>(.*?)</p></div></div></li>",
                    RegexOptions.Singleline);
            MatchCollection matches = query.Matches(data);
            List<Item> programs = new List<Item>();
            foreach (Match x in matches)
            {
                programs.Add(new Program(x.Groups[1].Value, x.Groups[2].Value, x.Groups[4].Value, x.Groups[3].Value));
            }
            return programs;
        }

        private IList<Item> GetFolders(int id)
        {
            string data = FetchUrl(PROGRAM_URL + id);
            Regex query =
                new Regex(
                    "<a id=\".*?\" href=\"/nett-tv/kategori/(.*?)\".*?title=\"(.*?)\".*?class=\"icon-(.*?)-black\".*?>(.*?)</a>");
            MatchCollection matches = query.Matches(data);
            List<Item> folders = new List<Item>();
            foreach (Match x in matches)
            {
                folders.Add(new Folder(x.Groups[1].Value, x.Groups[2].Value));
            }
            return folders;
        }

        public IList<Item> GetFolders(Program program)
        {
            return GetFolders(int.Parse(program.ID));
        }

        public IList<Item> GetFolders(Folder folder)
        {
            return GetFolders(int.Parse(folder.ID));
        }

        public IList<Item> GetFolders(Clip cl)
        {
            return GetFolders(cl.TilhoerendeProsjekt);
        }


        private IList<Item> GetClips(int id)
        {
            Log.Info("{0}: GetClips(int): {1}", NrkParserConstants.LIBRARY_NAME, id);
            string data = FetchUrl(PROGRAM_URL + id);
            List<Item> clips = new List<Item>();
            Regex query =
                new Regex("<a href=\"/nett-tv/klipp/(.*?)\"\\s+title=\"(.*?)\"\\s+class=\"(.*?)\".*?>(.*?)</a>");
            MatchCollection matches = query.Matches(data);
            foreach (Match x in matches)
            {
                Clip c = new Clip(x.Groups[1].Value, x.Groups[4].Value);
                c.TilhoerendeProsjekt = id;
                clips.Add(c);
            }

            return clips;
        }
        public IList<Item> GetClips(Program program)
        {
            Log.Info("{0}: GetClips(Program): {1}", NrkParserConstants.LIBRARY_NAME, program);
            return GetClips(Int32.Parse(program.ID));
        }

        public IList<Item> GetClipsTilhoerendeSammeProgram(Clip c)
        {
            //return GetClips(c.TilhoerendeProsjekt);
            List<Item> tItems = (List<Item>)GetClips(c.TilhoerendeProsjekt);
            tItems.AddRange(GetFolders(c.TilhoerendeProsjekt));
            return tItems;
        }

        public IList<Item> GetClips(Folder folder)
        {
            string data = FetchUrl(FOLDER_URL + folder.ID);

            List<Item> clips = new List<Item>();

            Regex query =
                new Regex("<a href=\"/nett-tv/klipp/(.*?)\"\\s+title=\"(.*?)\"\\s+class=\"(.*?)\".*?>(.*?)</a>");
            MatchCollection matches = query.Matches(data);
            foreach (Match x in matches)
            {
                clips.Add(new Clip(x.Groups[1].Value, x.Groups[4].Value));
            }

            return clips;
        }

        public string GetClipUrlAndPutStartTime(Clip clip)
        {
            Log.Debug("{0}: GetClipUrlAndPutStartTime(Clip): {1}", NrkParserConstants.LIBRARY_NAME, clip);

            if (clip.Type == Clip.KlippType.KLIPP)
            {
                return GetClipUrlAndPutStartTimeForKlipp(clip);
            }
            else if (clip.Type == Clip.KlippType.KLIPP_CHAPTER)
            {
                return clip.ID;
            }
            else if (clip.Type == Clip.KlippType.DIREKTE)
            {
                return GetClipUrlForDirekte(clip);
            }
            else if (clip.Type == Clip.KlippType.RSS)
            {
                return GetClipUrlForRSS(clip);
            }
            else if (clip.Type == Clip.KlippType.INDEX)
            {
                return GetClipUrlForIndex(clip);
            }
            else if (clip.Type == Clip.KlippType.NRKBETA || clip.Type == Clip.KlippType.PODCAST)
            {
                return clip.ID;
            }
            else
            {
                try
                {
                    return GetClipUrlForVerdi(clip);
                }
                catch(Exception e)
                {
                    Log.Error("Kunne ikke finne url til klipp", e);
                    return null;
                }
            }
        }

        private string GetClipUrlForDirekte(Clip clip)
        {
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Clip type is: " + clip.Type);
            string data = FetchUrl(DIREKTE_URL + clip.ID);
            Regex query;
            query = new Regex("<param name=\"FileName\" value=\"(.*?)\" />", RegexOptions.IgnoreCase);
            MatchCollection result = query.Matches(data);
            string urlToFetch;
            try
            {
                urlToFetch = result[0].Groups[1].Value;
            }
            catch (Exception e)
            {
                Log.Info("feilet: " + e.Message);
                return null;
            }
            string urldata = FetchUrl(urlToFetch);
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": " + urldata);
            query = new Regex("<ref href=\"(.*?)\" />");
            MatchCollection movie_url = query.Matches(urldata);

            //Usikker på en del av logikken for å hente ut url her. Ikke sikker på hvorfor
            //det var en try/catch
            //skip any advertisement
            String urlToReturn;
            try
            {
                urlToReturn = movie_url[1].Groups[1].Value;
                if (urlToReturn.ToLower().EndsWith(".gif"))
                {
                    Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Kan ikke spille av: " + urlToReturn + ", prøver annet treff.");
                    //vi kan ikke spille av fullt.gif, returnerer samme som i catch'en.
                    urlToReturn = movie_url[0].Groups[1].Value;
                }

            }
            catch
            {
                urlToReturn = movie_url[0].Groups[1].Value;
            }
            if (urlIsQTicketEnabled(urlToReturn))
            {
                urlToReturn = getDirectLinkWithTicket(urlToReturn);
            }
            return urlToReturn;
        }

        private string GetClipUrlAndPutStartTimeForKlipp(Clip clip)
        {
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Clip type is: " + clip.Type);
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Parsing xml: " + string.Format(NrkParserConstants.URL_GET_MEDIAXML, clip.ID, Speed));
            XmlKlippParser xmlKlippParser = new XmlKlippParser(string.Format(NrkParserConstants.URL_GET_MEDIAXML, clip.ID, Speed));
            string url = xmlKlippParser.GetUrl();
            clip.StartTime = xmlKlippParser.GetStartTimeOfClip();
            if (urlIsQTicketEnabled(url))
            {
                url = getDirectLinkWithTicket(url);
            }
            return url;
        }

        /// <summary>
        /// Method that returns the direct link to a qticket enabled clip, including the ticket
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Direct link to the clip</returns>
        private string getDirectLinkWithTicket(string url)
        {
            String data = FetchUrl(url);
            Regex query = new Regex("<ref href=\"(.*?)\"/>", RegexOptions.IgnoreCase);
            MatchCollection result = query.Matches(data);
            string ticketEnabledUrl = result[0].Groups[1].Value;
            return ticketEnabledUrl;         
        }

        /// <summary>
        /// Method that returns true if the url-string contains "qticket"
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool urlIsQTicketEnabled(string url)
        {
            return url.ToLower().Contains(NrkParserConstants.QTICKET);
        }

        private static string GetClipUrlForRSS(Clip clip)
        {
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Clip type is RSS");
            return NrkParserConstants.RSS_CLIPURL_PREFIX + clip.ID;
        }

        private string GetClipUrlForIndex(Clip clip)
        {
            Regex query;
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Clip type is INDEX");
            string data = FetchUrl(INDEX_URL + clip.ID);
            query = new Regex("<param name=\"FileName\" value=\"(.*?)\" />", RegexOptions.IgnoreCase);
            MatchCollection result = query.Matches(data);
            string urlToFetch = result[0].Groups[1].Value;
            urlToFetch = urlToFetch.Replace("amp;", ""); //noen ganger er det amper der..som må bort
            string urldata = FetchUrl(urlToFetch);
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": " + urldata);
            query = new Regex("<starttime value=\"(.*?)\" />.*?<ref href=\"(.*?)\" />", RegexOptions.Singleline);
            MatchCollection movie_url = query.Matches(urldata);
            //skip any advertisement

            string str_startTime = movie_url[0].Groups[1].Value;
            Log.Debug(NrkParserConstants.LIBRARY_NAME + ": Starttime er: " + str_startTime);
            //må gjøre string representasjon på formen: 00:27:38, om til en double
            Double dStartTime = NrkUtils.convertToDouble(str_startTime);
            clip.StartTime = dStartTime;
            return movie_url[0].Groups[2].Value;
        }

        private string GetClipUrlForVerdi(Clip clip)
        {
            Log.Info(NrkParserConstants.LIBRARY_NAME + ": Fetching verdi url");
            string data = FetchUrl(STORY_URL + clip.ID);
            
            Regex query = new Regex("<media:content url=\"(.*?)&amp;app=nett-tv\"", RegexOptions.IgnoreCase);
            MatchCollection result = query.Matches(data);
            string urlToFetch = result[0].Groups[1].Value;
            return urlToFetch;
        }

        private string getUrl(String cateogry)
        {
            if (cateogry.ToLower().Equals("nyheter"))
                return MAIN_URL + "nyheter";
            else if (cateogry.ToLower().Equals("sport"))
                return MAIN_URL + "sport";
            else if (cateogry.ToLower().Equals("distrikt"))
                return MAIN_URL + "distrikt";
            else if (cateogry.ToLower().Equals("natur"))
                return MAIN_URL + "natur";
            else
                throw new Exception("No valid URL found!");
        }

        private string FetchUrl(string url)
        {
            return httpClient.GetUrl(url);
        }

        public enum Periode
        {
            Uke,
            Maned,
            Totalt
        }

        public IList<Clip> getChapters(Clip item)
        {
            XmlKlippParser xmlKlippParser = new XmlKlippParser(string.Format(NrkParserConstants.URL_GET_MEDIAXML, item.ID, Speed));
            return xmlKlippParser.GetChapters();
        }

        public IList<PodKast> GetVideoPodkaster()
        {            
            string videokastsSectionAsString = GetVideokastsSectionAsString();
            return CreatePodkasts(videokastsSectionAsString);
        }

        private static IList<PodKast> CreatePodkasts(string videokastsSectionAsString)
        {
            string tbodyExpression = "<tbody>(.*?)</tbody>";
            Regex queryTbodys = new Regex(tbodyExpression, RegexOptions.Singleline);
            MatchCollection matches = queryTbodys.Matches(videokastsSectionAsString);
            IList<PodKast> items = new List<PodKast>();
            foreach (Match x in matches)
            {
                string urlExpressionString =
                    "<tbody>.*?<tr class=\"pod-row\">.*?<th>(?<title>[^</]*)</th>.*?<td class=\"pod-rss\">.*?</td>.*?</tr>.*?<tr class=\"pod-desc\">.*?<td colspan=\"3\">.*?<p>(?<description>[^<]*)<a href=\".*?\" title=\".*?\">.*?</a></p>.*?</td>.*?</tr>.*?<tr class=\"pod-rss-url\">.*?<td colspan=\"3\">.*?<a href=\"(?<url>[^\"]*)\" title=\".*?\">.*?</a>.*?</td>.*?</tr>.*?</tbody>";
                PodKast kast = CreatePodkastItem(urlExpressionString, x.Groups[0].Value);
                if (kast != null)
                {
                    items.Add(kast);
                }

            }
            return items;
        }

        private static PodKast CreatePodkastItem(string urlExpressionString, string videokastsSectionAsString)
        {
            Regex queryVideokasts = new Regex(urlExpressionString, RegexOptions.Singleline);
            MatchCollection matches = queryVideokasts.Matches(videokastsSectionAsString);
            if (matches.Count == 1)
            {
                PodKast item = new PodKast(matches[0].Groups["url"].Value, matches[0].Groups["title"].Value);
                item.Description = matches[0].Groups["description"].Value;
                item.Bilde = String.Empty;
                return item;
            }
            return null;
            
        }

        private string GetVideokastsSectionAsString()
        {
            string videokastsExpression =
                "<table summary=\"Liste over videopodkaster fra NRK\">(?<media>[^</table>].*)</table>.*?</div>.*?<div class=\"pod\">.*?<table summary=\"Liste over lydpodkaster fra NRK\">";
            return GetMediaSection(videokastsExpression);
        }

        private string GetMediaSection(string videokastsExpression)
        {
            String pageAsString = FetchUrl("http://www.nrk.no/podkast/");       
            Regex queryVideokastsSection = new Regex(videokastsExpression, RegexOptions.Singleline);
            MatchCollection matches = queryVideokastsSection.Matches(pageAsString);
            return matches[0].Groups["media"].Value;
        }

        private string GetLydkastsSectionAsString()
        {
            string videokastsExpression =
                "<table summary=\"Liste over lydpodkaster fra NRK\">(?<media>[^</table>].*)</table>.*?</div>";
            return GetMediaSection(videokastsExpression);
        }

        public IList<PodKast> GetLydPodkaster()
        {
            string lydkastSectionAsString = GetLydkastsSectionAsString();
            return CreatePodkasts(lydkastSectionAsString);
        }

        public IList<Item> getAnbefalte()
        {
            String data = FetchUrl(MAIN_URL);
            IList<Item> clips = new List<Item>();
            String strQuery =
                "<div><a href=\".*?/nett-tv/(?<type>[^/]*)/(?<id>[^\"]*)\".*?><img src=\"(?<imgsrc>[^\"]*)\" alt=\".*?\" width=\"150\" height=\"85\" /></a><h3><a href=\".*?\" title=\".*?\">(?<title>[^<]*)</a></h3><div class=\"summary\"><p>(?<desc>[^<]*)</p></div>.*?</div>";
            Regex query =
                new Regex(strQuery);
            MatchCollection matches = query.Matches(data);
            foreach (Match x in matches)
            {
                String id = x.Groups["id"].Value;
                id = id.Replace('/', ' ').Trim();
                Clip c = new Clip(id, x.Groups["title"].Value);
                c.Description = x.Groups["desc"].Value;
                c.Bilde = x.Groups["imgsrc"].Value;
                if ("klipp".Equals(x.Groups["type"].Value))
                {
                    c.Type = Clip.KlippType.KLIPP;
                }
                else
                {
                    c.Type = Clip.KlippType.INDEX;
                }
                clips.Add(c);
            }

            return clips;
        }
    }


}