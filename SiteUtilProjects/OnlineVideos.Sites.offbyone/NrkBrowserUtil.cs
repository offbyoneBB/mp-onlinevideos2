using System;
using System.Collections.Generic;
using System.ComponentModel;
using Vattenmelon.Nrk.Browser;
using Vattenmelon.Nrk.Browser.Translation;
using Vattenmelon.Nrk.Parser;
using Vattenmelon.Nrk.Domain;
using Vattenmelon.Nrk.Parser.Xml;

namespace OnlineVideos.Sites
{
    public class NrkBrowserUtil : SiteUtilBase
    {
        public enum VideoQuality { Low, Medium, High };

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the quality for videos and livestreams.")]
        VideoQuality videoQuality = VideoQuality.Medium;

        protected NrkParser nrkParser = null;
        protected string liveStreamUrlSuffix = NrkBrowserConstants.QUALITY_MEDIUM_SUFFIX;
        protected int speed = NrkBrowserConstants.PREDEFINED_MEDIUM_SPEED;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            switch (videoQuality)
            {
                case VideoQuality.Low:
                    liveStreamUrlSuffix = NrkBrowserConstants.QUALITY_LOW_SUFFIX;
                    speed = NrkBrowserConstants.PREDEFINED_LOW_SPEED;
                    break;
                case VideoQuality.Medium:
                    liveStreamUrlSuffix = NrkBrowserConstants.QUALITY_MEDIUM_SUFFIX;
                    speed = NrkBrowserConstants.PREDEFINED_MEDIUM_SPEED;
                    break;
                case VideoQuality.High:
                    liveStreamUrlSuffix = NrkBrowserConstants.QUALITY_HIGH_SUFFIX;
                    speed = NrkBrowserConstants.PREDEFINED_HIGH_SPEED;
                    break;
            }
        }

        public override int DiscoverDynamicCategories()
        {
            if (nrkParser == null)
            {
                Log.Info("NrkParser is null, getting speed-cookie and creating parser.");
                nrkParser = new NrkParser(speed);
            }

            Settings.Categories = new BindingList<Category>();

            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_ALPHABETICAL_LIST, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_ALPHABETICAL_LIST, 
                                                    HasSubCategories = true });
            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_CATEGORIES, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_CATEGORIES, 
                                                    HasSubCategories = true });
            
            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_LIVE, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_LIVE_STREAMS });
            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_LIVE_ALTERNATE, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_ALTERNATIVE_LINKS });
            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_CHOOSE_STREAM_MANUALLY, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_CHOOSE_STREAM_MANUALLY });

            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_LATEST_CLIPS, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_LATEST_CLIPS, 
                                                    Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTION_LATEST_CLIPS });
            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_RECOMMENDED_PROGRAMS, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_RECOMMENDED_PROGRAMS, 
                                                    Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTION_RECOMMENDED_PROGRAMS, 
                                                    /*Thumb = "nrkbrowser\\" + NrkBrowserConstants.NRK_LOGO_PICTURE*/ });

            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_MOST_WATCHED, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_MOST_WATCHED, 
                                                    Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTION_MOST_WATCHED, 
                                                    HasSubCategories = true });                        

            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA,
                                                    Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTON_NRKBETA, 
                                                    /*Thumb = "nrkbrowser\\" + NrkBrowserConstants.MENU_ITEM_PICTURE_NRKBETA,*/
                                                    HasSubCategories = true });
            
            Settings.Categories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_PODCASTS_VIDEO, 
                                                    Name = NrkTranslatableStrings.MENU_ITEM_TITLE_PODCASTS,
                                                    /*Thumb = "nrkbrowser\\" + NrkBrowserConstants.NRK_LOGO_PICTURE,*/
                                                    HasSubCategories = true});

            Settings.DynamicCategoriesDiscovered = true;
            
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            string id = ((RssLink)parentCategory).Url;            

            if (id == NrkBrowserConstants.MENU_ITEM_ID_ALPHABETICAL_LIST)
            {
                foreach(Program prg in nrkParser.GetAllPrograms())
                {
                    parentCategory.SubCategories.Add(new RssLink() { Url = prg.ID, Name = prg.Title, Description = prg.Description, Thumb = prg.Bilde, Other = prg, HasSubCategories = true, ParentCategory = parentCategory });
                }
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_CATEGORIES)
            {
                foreach (Vattenmelon.Nrk.Domain.Category cat in nrkParser.GetCategories())
                {
                    parentCategory.SubCategories.Add(new RssLink() { Url = cat.ID, Name = cat.Title, Other = cat, HasSubCategories = true, ParentCategory = parentCategory });
                }
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_MOST_WATCHED)
            {
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_MEST_SETTE_UKE, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_MEST_SETTE_UKE, Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTION_MEST_SETTE_UKE, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_MEST_SETTE_MAANED, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_MEST_SETTE_MAANED, Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTION_MEST_SETTE_MAANED, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_MEST_SETTE_TOTALT, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_MEST_SETTE_TOTALT, Description = NrkTranslatableStrings.MENU_ITEM_DESCRIPTION_MEST_SETTE_TOTALT, ParentCategory = parentCategory });
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA)
            {
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_SISTE_KLIPP, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_LATEST_CLIPS, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_TVSERIER, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_TV_SERIES, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_DIVERSE, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_OTHER, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_PRESENTASJONER, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_PRESENTATIONS, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_KONFERANSER_OG_MESSER, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_CONFERENCES, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_FRA_TV, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_FROM_TV, ParentCategory = parentCategory });
                parentCategory.SubCategories.Add(new RssLink() { Url = NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_HD_KLIPP, Name = NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA_HD_CLIPS, ParentCategory = parentCategory });                
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_PODCASTS_VIDEO)
            {
                foreach (PodKast pk in nrkParser.GetVideoPodkaster())
                {
                    parentCategory.SubCategories.Add(new RssLink() { Url = pk.ID, Name = pk.Title, Description = pk.Description, Thumb = pk.Bilde, Other = pk, ParentCategory = parentCategory });
                }
            }
            else if (parentCategory.Other is Vattenmelon.Nrk.Domain.Category)
            {
                foreach (Program prg in nrkParser.GetPrograms((Vattenmelon.Nrk.Domain.Category)parentCategory.Other))
                {
                    parentCategory.SubCategories.Add(new RssLink() { Url = prg.ID, Name = prg.Title, Description = prg.Description, Thumb = prg.Bilde, Other = prg, HasSubCategories = true, ParentCategory = parentCategory });
                }
            }
            else if (parentCategory.Other is Program)
            {                
                foreach (Folder f in nrkParser.GetFolders((Program)parentCategory.Other))
                {                    
                    parentCategory.SubCategories.Add(new RssLink() { Url = f.ID, Name = f.Title, Other = f, ParentCategory = parentCategory });
                }
                if (parentCategory.SubCategories.Count == 0)
                {
                    // if no folder found, create a Category for "All"
					parentCategory.SubCategories.Add(new RssLink() { Name = Translation.Instance.All, Other = parentCategory.Other, ParentCategory = parentCategory });
                }
            }

            parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            IList<Item> items = null;
            string id = ((RssLink)category).Url;

            if (id == NrkBrowserConstants.MENU_ITEM_ID_LIVE)
            {
                items = nrkParser.GetDirektePage();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_LIVE_ALTERNATE)
            {
                items = new List<Item>();
                items.Add(new Stream(NrkParserConstants.STREAM_PREFIX + "03" + liveStreamUrlSuffix, NrkBrowserConstants.MENU_ITEM_LIVE_ALTERNATE_NRK1));
                items.Add(new Stream(NrkParserConstants.STREAM_PREFIX + "04" + liveStreamUrlSuffix, NrkBrowserConstants.MENU_ITEM_LIVE_ALTERNATE_NRK2));
                items.Add(new Stream(NrkParserConstants.STREAM_PREFIX + "05" + liveStreamUrlSuffix, NrkBrowserConstants.MENU_ITEM_LIVE_ALTERNATE_3));
                items.Add(new Stream(NrkParserConstants.STREAM_PREFIX + "08" + liveStreamUrlSuffix, NrkBrowserConstants.MENU_ITEM_LIVE_ALTERNATE_4));                
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_CHOOSE_STREAM_MANUALLY)
            {
                items = new List<Item>();
                for (int i = 0; i < 10; i++)
                {
                    items.Add(new Stream(NrkParserConstants.STREAM_PREFIX + i.ToString("D2") + liveStreamUrlSuffix, "Strøm " + i));
                }
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_LATEST_CLIPS)
            {
                items = nrkParser.GetSistePaaForsiden();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_RECOMMENDED_PROGRAMS)
            {
                items = nrkParser.getAnbefalte();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_MEST_SETTE_UKE)
            {
                items = nrkParser.GetMestSette(7);
                foreach (Item i in items) i.Description = String.Format(NrkTranslatableStrings.DESCRIPTION_CLIP_SHOWN_TIMES, i.Description);                
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_MEST_SETTE_MAANED)
            {
                items = nrkParser.GetMestSette(31);
                foreach (Item i in items) i.Description = String.Format(NrkTranslatableStrings.DESCRIPTION_CLIP_SHOWN_TIMES, i.Description);                
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_MEST_SETTE_TOTALT)
            {
                items = nrkParser.GetMestSette(3650);
                foreach (Item i in items) i.Description = String.Format(NrkTranslatableStrings.DESCRIPTION_CLIP_SHOWN_TIMES, i.Description);                
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_SISTE_KLIPP)
            {
                items = new NrkBetaXmlParser().FindLatestClips().getClips();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_TVSERIER || id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_FRA_TV)
            {
                NrkBetaXmlParser nrkBetaParser = new NrkBetaXmlParser(NrkParserConstants.NRK_BETA_FEEDS_KATEGORI_URL, NrkParserConstants.NRK_BETA_SECTION_FRA_TV);
                items = nrkBetaParser.getClips();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_DIVERSE)
            {
                NrkBetaXmlParser nrkBetaParser = new NrkBetaXmlParser(NrkParserConstants.NRK_BETA_FEEDS_KATEGORI_URL, NrkParserConstants.NRK_BETA_SECTION_DIVERSE);
                items = nrkBetaParser.getClips();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_KONFERANSER_OG_MESSER)
            {
                NrkBetaXmlParser nrkBetaParser = new NrkBetaXmlParser(NrkParserConstants.NRK_BETA_FEEDS_KATEGORI_URL, NrkParserConstants.NRK_BETA_SECTION_KONFERANSER_OG_MESSER);
                items = nrkBetaParser.getClips();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_PRESENTASJONER)
            {
                NrkBetaXmlParser nrkBetaParser = new NrkBetaXmlParser(NrkParserConstants.NRK_BETA_FEEDS_KATEGORI_URL, NrkParserConstants.NRK_BETA_SECTION_PRESENTASJONER);
                items = nrkBetaParser.getClips();
            }
            else if (id == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA_HD_KLIPP)
            {
                items = new NrkBetaXmlParser().FindHDClips().getClips();
            }
            else if (category.Other is Program)
            {
                items = nrkParser.GetClips((Program)category.Other);
            }
            else if (category.Other is Folder)
            {
                items = nrkParser.GetClips((Folder)category.Other);                
            }
            else if (category.Other is PodKast)
            {
                PodkastXmlParser pxp = new PodkastXmlParser(id);
                items = pxp.getClips();                
            }

            return VideosFromItems(items);
        }

        public override List<String> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> urls = new List<string>();
            if (video.VideoUrl.StartsWith("mms://"))
            {
                urls.Add(video.VideoUrl);
            }
            else
            {
                Clip item = video.Other as Clip;
                if (item == null && video.Other is string)
                {
                    string[] infos = ((string)video.Other).Split('|');
                    if (infos.Length == 2)
                    {
                        item = new Clip(infos[0], "") { Type = (Clip.KlippType)Enum.Parse(typeof(Clip.KlippType), infos[1]) };
                    }
                }
                if (item != null)
                {                    
                    urls.Add(nrkParser.GetClipUrlAndPutStartTime(item));
                    video.StartTime = TimeSpan.FromSeconds(item.StartTime).ToString();
                }
            }
            return urls;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add(NrkTranslatableStrings.MENU_ITEM_TITLE_NRKBETA, NrkBrowserConstants.MENU_ITEM_ID_NRKBETA);
            return result;
        }

        int currentSearchResultsPage = 0;
        string currentSearchString = "";

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            if (category == NrkBrowserConstants.MENU_ITEM_ID_NRKBETA)
            {
                NrkBetaXmlParser parser = new NrkBetaXmlParser();
                parser.SearchFor(query);
                return VideosFromItems(parser.getClips()).ConvertAll<SearchResultItem>(v => v as SearchResultItem);
            }
            else
            {
                currentSearchResultsPage = 0;
                currentSearchString = query;
                return Search().ConvertAll<SearchResultItem>(v => v as SearchResultItem);
            }
        }
        
        public override List<VideoInfo> GetNextPageVideos()
        {
            currentSearchResultsPage++;
            return Search();
        }
        
        List<VideoInfo> Search()
        {
            IList<Item> matchingItems = nrkParser.GetSearchHits(currentSearchString, currentSearchResultsPage);
            List<VideoInfo> result = VideosFromItems(matchingItems);
            HasNextPage = matchingItems.Count >= 25;
            return result;
        }

        #endregion

        List<VideoInfo> VideosFromItems(IList<Item> items)
        {
            HasNextPage = false;            
            List<VideoInfo> result = new List<VideoInfo>();
            if (items != null)
            {
                foreach (Item item in items)
                {
                    if (item is Clip)
                    {
                        Clip clip = item as Clip;
                        VideoInfo vi = new VideoInfo() { Title = clip.Title, Description = clip.Description, ImageUrl = clip.Bilde, Length = clip.Duration, VideoUrl = clip.ID, Other = clip };
                        result.Add(vi);

                        if (((Clip)item).Type == Clip.KlippType.KLIPP)
                        {
                            IList<Clip> chapters = nrkParser.getChapters((Clip)item);
                            for(int i = 0; i<chapters.Count; i++)
                            {
                                Clip chapter_clip = chapters[i];                                
                                result.Add(new VideoInfo() 
                                { 
                                    Title = string.Format("{0} ({1}/{2} - {3})", clip.Title, (i+1).ToString(), chapters.Count.ToString(), chapter_clip.Title), 
                                    Description = chapter_clip.Description, 
                                    ImageUrl = chapter_clip.Bilde, 
                                    Length = chapter_clip.Duration, 
                                    VideoUrl = chapter_clip.ID, 
                                    Other = chapter_clip, 
                                    StartTime = TimeSpan.FromSeconds(chapter_clip.StartTime).ToString() 
                                });
                            }
                        }
                    }
                    else if (item is Stream)
                    {
                        Stream stream = item as Stream;
                        VideoInfo vi = new VideoInfo() { Title = stream.Title, VideoUrl = stream.ID, ImageUrl = stream.Bilde, Description = stream.Description };
                        result.Add(vi);
                    }
                }
            }
            return result;
        }
    }
}
