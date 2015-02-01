using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class AllocineUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            RssLink cat = new RssLink();
            cat.Url = "http://www.allocine.fr/video/bandes-annonces/";
            cat.Name = "Bandes-annonces";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.allocine.fr/video/emissions/";
            cat.Name = "Emissions Allociné";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.allocine.fr/video/series/";
            cat.Name = "Vidéos de séries TV";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.allocine.fr/video/interviews/";
            cat.Name = "Interviews de stars";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            return 4;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Name.Equals("Bandes-annonces"))
            {
                parentCategory.SubCategories = new List<Category>();

                RssLink cat = new RssLink();
                cat.Url = "http://www.allocine.fr/video/bandes-annonces/";
                cat.Name = "A ne pas manquer";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.allocine.fr/video/bandes-annonces/meilleures/";
                cat.Name = "Les meilleures bandes-annonces";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.allocine.fr/video/bandes-annonces/films-au-cinema/?tri=1";
                cat.Name = "Films au cinéma";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.allocine.fr/video/bandes-annonces/films-prochainement/?tri=1";
                cat.Name = "Agenda";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.allocine.fr/video/bandes-annonces/toutes/?tri=1";
                cat.Name = "Toutes les bandes-annonces";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                return 5;
            }
            return base.DiscoverSubCategories(parentCategory);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
           RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

           if (category.ParentCategory != null && category.ParentCategory.Name.Equals("Emissions Allociné"))
            {
                videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)"">\s*<img\ssrc='(?<ImageUrl>[^']*)'\salt=""[^""]*""\s*title=""[^""]*""\s/>\s*</a>\s*</div>\s*<div\sclass=""contenzone"">\s*<div\sclass=""titlebar"">\s*<a\shref='[^']*'\sclass=""link"">\s*<span\sclass='bold'>(?<Title>[^<]*)</span></a><br/>\s*</div>\s*<ul\sclass=""[^""]*"">(?<Description>.*?)<div\sclass=""spacer""></div>\s*</div>";
                //videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)"">\s*<img\ssrc='(?<ImageUrl>[^']*)'\salt=""[^""]*""\s*title=""[^""]*""\s/>\s*</a>\s*</div>\s*<div\sclass=""contenzone"">.*?<br/>(?<Title>[^<]*)</a>\s*<ul\sclass=""[^""]*""\sstyle=""[^""]*""\s>(?<description>[^<]*)</ul>";
                //videoListRegEx = @"<a\ href=""?'?(?<VideoUrl>/video/player[^""']*)""?'?(?:(?!>).)*>[^<]*<img\ src='(?<ImageUrl>[^']*)'(?:(?!alt).)*alt=""Photo\ \:\ (?<Title>[^""]*)""";
                
            }
           else if (category.ParentCategory != null && category.ParentCategory.Name.Equals("Bandes-annonces"))
           {
               List<VideoInfo> videoList = new List<VideoInfo>();
               if (category.Name.Equals("Les meilleures bandes-annonces"))
               {
                   videoListRegEx = @"<div\sclass=""movie_[^""]*"">\s*<a\shref='(?<VideoUrl>[^']*)'>\s*<img\ssrc='(?<ImageUrl>[^']*)'\s*alt=""[^""]*""\s*title=""[^""]*""\s/>\s*</a><a\shref='[^']*'>\s*<strong>(?<Title>[^<]*)</strong>(?<Description>[^<]*)</a><span>&nbsp;</span>\s*</div>";
                   regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
                   videoList.AddRange(base.GetVideos(category));
                   videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)""\sid=""[^""]*"">\s*<img\ssrc='(?<ImageUrl>[^']*)'\s*alt=""[^""]*""\s*title=""[^""]*""\s/>\s*</a>\s*</div><!--/picturezone-->\s*<div\sclass=""titlebar"">\s*<a\sclass=""link""\shref=""[^""]*""\sid=""[^""]*"">\s*<span>[^<]*</span><strong>(?<Title>[^<]*)</strong>\s<br\s/>(?<Description>[^<]*)</a>";
                   regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
                   videoList.AddRange(base.GetVideos(category));
                   
               }
               if (category.Name.Equals("A ne pas manquer"))
               {
                   videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)"">\s*<img\ssrc='(?<ImageUrl>[^']*)'\salt=""[^""]*""\s*title=""[^""]*""\s/>\s*</a>\s*</div>\s*<div\sclass=""contenzone"">\s*<div\sclass=""titlebar"">\s*<a.*?href=[^>]*>\s*<strong>(?<Title>[^<]*)</strong>(?<Description>[^<]*)";
                   regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
                   videoList.AddRange(base.GetVideos(category));
               }

               //videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)"">\s*<img\ssrc='(?<ImageUrl>[^']*)'\salt=""[^""]*""\s*title=""[^""]*""\s/>\s*</a>\s*</div>\s*<div\sclass=""contenzone"">\s*<div\sclass=""titlebar"">\s*<a.*?href=[^>]*>\s*<strong>(?<Title>[^<]*)</strong>(?<Description>[^<]*)";
               videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)"">\s*<img\ssrc='(?<ImageUrl>[^']*)'.*?<div\sclass=""contenzone"">\s*<div\sclass=""titlebar"">\s*<a.*?href=[^>]*>\s*<span\sclass='bold'>(?<Title>[^<]*)</span>(?<Description>[^<]*)";
               regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
               videoList.AddRange(base.GetVideos(category));
               return videoList;
            } 
           else
            {
                videoListRegEx = @"<div\sclass=""picturezone"">\s*<a\shref=""(?<VideoUrl>[^""]*)"">\s*<img\ssrc='(?<ImageUrl>[^']*)'.*?<div\sclass=""contenzone"">\s*<div\sclass=""titlebar"">\s*<a.*?href=[^>]*>\s*<span\sclass='bold'>(?<Title>[^<]*)</span>(?<Description>[^<]*)";
            }
            regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);

            return base.GetVideos(category);
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> listUrls = new List<string>();

            string webData = GetWebData(video.VideoUrl);
            string id = Regex.Match(webData, @"{""refmedia"":(?<url>[^,]*)").Groups["url"].Value;

            webData = GetWebData(@"http://www.allocine.fr/video/xml/videos.asp?media=" + id + @"&hd=1");
       
            string url = Regex.Match(webData, @"<video\s+title="".*?""\s+xt_title="".*?""\s+ld_path=""(?<ld>[^""]*)""\s+md_path=""(?<md>[^""]*)""\s+hd_path=""(?<hd>[^""]*)"".*?/>").Groups["hd"].Value;

            if (url == null || url.Length == 0)
            {
                url = Regex.Match(webData, @"<video\s+title="".*?""\s+xt_title="".*?""\s+ld_path=""(?<ld>[^""]*)""\s+md_path=""(?<md>[^""]*)""\s+hd_path=""(?<hd>[^""]*)"".*?/>").Groups["md"].Value;
            }
            if (url == null || url.Length == 0)
            {
                url = Regex.Match(webData, @"<video\s+title="".*?""\s+xt_title="".*?""\s+ld_path=""(?<ld>[^""]*)""\s+md_path=""(?<md>[^""]*)""\s+hd_path=""(?<hd>[^""]*)"".*?/>").Groups["ld"].Value;
            }
            if (url != null && url.Length > 0)
            {
                listUrls.Add(new MPUrlSourceFilter.HttpUrl(@"http://a69.g.akamai.net/n/69/32563/v1/mediaplayer.allocine.fr" + url + ".flv") { UserAgent = OnlineVideoSettings.Instance.UserAgent }.ToString());
            }
            return listUrls;
        }
    }
}
