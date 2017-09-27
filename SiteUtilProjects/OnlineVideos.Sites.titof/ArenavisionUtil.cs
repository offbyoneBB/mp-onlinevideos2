using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites
{
    public class ArenavisionUtil : GenericSiteUtil
    {
        #region Private Fields

        private static List<ArenaVisionEPG> _Epg = new List<ArenaVisionEPG>();

        private static System.Threading.Thread _thread; 
        private static string[] _Link = new string[]
                {
                    "",
                "fb3a568b39ae64980405da468b5b40e98f29af33",
                "b3ec490889eee41211058cd0bdce88be013825a5",
                "b5950a56db8f722876dc74443d74b565fb99368f",
                "c96b488be623f1d9c3d247cd3a4130778ff9013a",
                "5d25598468b68aabc1d908921cea98062c7f8739",
                "ec93f920a03bc4a95060614b514cc2df1aa4cd9e",
                "37c1dd5711ffcc688314634b9d093e959d759a0f",
                "f25e26a4bc181ddb12129480bc51d302b8543c5b",
                "97f0eaa031804b7c9f5b7f60599047254d9128b1",
                "6c869c22127b6056d99b5ab1cf9303990e16114c",
                "304e94a06ed5efa29eb3b670fec51f5db9cb0d7e",
                "84286fbf9799f31b289a2983611a5b3232f85a6b",
                "9da7abe6f7427590f1562cbc6fc5a940b3ad3bcc",
                "0a0be3253e0374f5f6323391c62b244eed5673c6",
                "a42c3b27114341f7245eff8e174680c515d9a9fa",
                "4cd2cd0afef8d2ad150175afe3404a3a7ddae3c6",
                "b3162b881c0efa21edbffcab93afa655027b2a1d",
                "a1d6ef7a25009ae5d76f2538aa205e8bd935970c",
                "c6b0f8c3ff1b6f565886c70ef0812afcff5c3623",
                "399d25e4a05fc4ccd01d92d000d75000a40a7878"
                };

        #endregion Private Fields

        #region Public Methods

        public override int DiscoverDynamicCategories()
        {
            _thread = new System.Threading.Thread(new System.Threading.ThreadStart(ReadEPG));
            _thread.Start();
 
            Settings.Categories.Clear();
            RssLink cat = null;

            cat = new RssLink()
            {
                Name = "Channels",
                Other = "channels",
                Thumb = "http://arenavision.in/sites/default/files/FAVICON_AV2015.png",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Name = "Agenda",
                Other = "agenda",
                Thumb = "http://arenavision.in/sites/default/files/FAVICON_AV2015.png",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (_thread != null)
                _thread.Join();

            List<VideoInfo> tVideos = new List<VideoInfo>();
            if (category.Name == "Agenda")
            {
                var tlistAgenda = from itemEpg in _Epg
                            where itemEpg.Date.CompareTo(DateTime.Now.AddMinutes(90)) < 0
                            orderby itemEpg.Date descending
                            select itemEpg;
                
                if (tlistAgenda.Count() > 0) 
                {
                    foreach (ArenaVisionEPG agenda in _Epg)
                    {
                        string[] tStream = agenda.Stream.Split('-');
                        foreach (string stream in tStream )
                        {
                            if (!string.IsNullOrEmpty(stream)) 
                            {
                                int iStream = 0;
                                int.TryParse(stream, out iStream);
                                if (iStream >0 && iStream < 21)
                                {
                                    VideoInfo item = new VideoInfo()
                                    {
                                        StartTime = agenda.Date.TimeOfDay.ToString(),
                                        Title = "[AV"+iStream+ "] "+agenda.Event.Replace("\n\t", " "),
                                        Description = agenda.Date.TimeOfDay.ToString() + " - " +agenda.Sport + " " + agenda.Competition + " " + agenda.Event,
                                        Thumb = "http://arenavision.in/sites/default/files/FAVICON_AV2015.png",
                                        VideoUrl = "av" + iStream //string.Format("http://127.0.0.1:6878/ace/getstream?id={0}&hlc=1&spv=0&transcode_audio=0&transcode_mp3=0&transcode_ac3=0&preferred_audio_language=eng", _Link[iStream]),
                                    };

                                    tVideos.Add(item);
                                }
                                
                            }
                        }
                        tStream = null;
                    } 
                }
            }
            else
            {
                
                for (int idx = 1; idx <= 30; idx++)
                {
                    VideoInfo item = new VideoInfo()
                    {
                        Title = "ArenaVision " + idx,
                        Thumb = "http://arenavision.in/sites/default/files/FAVICON_AV2015.png",
                        VideoUrl = "av"+ idx //string.Format("http://127.0.0.1:6878/ace/getstream?id={0}&hlc=1&spv=0&transcode_audio=0&transcode_mp3=0&transcode_ac3=0&preferred_audio_language=eng", _Link[idx]),
                    };

                    var tlist = from itemEpg in _Epg
                                where itemEpg.Date.CompareTo(DateTime.Now.AddMinutes(90)) < 0
                                    && itemEpg.Stream.Contains("-" + idx.ToString() + "-")
                                orderby itemEpg.Date descending
                                select itemEpg;
                    var found = tlist.FirstOrDefault();
                    if (found != null)
                    {
                        item.StartTime = found.Date.ToString();
                        item.Description = found.Sport + " " + found.Competition + " " + found.Event;
                    }
                    tVideos.Add(item);
                }
            }
            return tVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string webdata = GetWebData(@"http://arenavision.in/"+video.VideoUrl );

            int start= webdata.IndexOf("acestream://")+12;
            int stop = webdata.IndexOf("\"",start );
            string sContentId = webdata.Substring(start, stop-start );

            string nfo = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            string surl = string.Format("http://127.0.0.1:6878/ace/getstream?id={0}&hlc=1&spv=0&transcode_audio=0&transcode_mp3=0&transcode_ac3=0&preferred_audio_language={1}", sContentId,nfo);
                                    
            return surl;
        }

        #endregion Public Methods

        #region Private Methods

        private void ReadEPG()
        {
            string agendaurl = "http://arenavision.in/schedule";
            string content = GetWebData(agendaurl);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

            try
            {
                doc.LoadHtml(content);
                var table = doc.DocumentNode.SelectSingleNode("//table");
                _Epg.Clear();
                for (int idx = 1; idx < table.ChildNodes.Count(); idx++)
                {
                    try
                    {
                        var element = table.ChildNodes[idx];

                        string date = element.ChildNodes[0].InnerText;
                        string hour = element.ChildNodes[2].InnerText.Replace("CET", "");
                        DateTime dte;
                        DateTime.TryParse(date + " " + hour, out dte);

                        ArenaVisionEPG item = new ArenaVisionEPG()
                        {
                            Date = dte,
                            Competition = element.ChildNodes[6].InnerText,
                            Sport = element.ChildNodes[4].InnerText,
                            Event = element.ChildNodes[8].InnerText,
                            Stream = "-" + element.ChildNodes[10].InnerText.Replace(" ", "-").Replace("\t", "-").Replace("\n", "-"),
                        };

                        _Epg.Add(item);
                    }
                    catch { }
                }

            }
            catch (Exception ex)
            {

            }
            
            doc = null;
        }

        #endregion Private Methods

        #region Internal Classes

        internal class ArenaVisionEPG
        {
            #region Internal Properties

            internal string Competition { get; set; }

            /// <summary>
            ///
            /// </summary>
            internal DateTime Date { get; set; }

            internal string Event { get; set; }

            /// <summary>
            ///
            /// </summary>
            internal string Sport { get; set; }
            internal string Stream { get; set; }

            #endregion Internal Properties
        }

        #endregion Internal Classes
    }
}