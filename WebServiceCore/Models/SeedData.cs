using Microsoft.EntityFrameworkCore;
using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            // Initialize the database with values for testing during development
            using (var context = new OnlineVideosDataContext(serviceProvider.GetRequiredService<DbContextOptions<OnlineVideosDataContext>>()))
            {
                // Database already seeded?
                if (context.Sites.Any() || context.Users.Any() || context.Reports.Any() || context.Dlls.Any())
                    return;

                context.Users.AddRange(
                    new User
                    {
                        Email = "user@example.com",
                        Password = "123456",
                    },
                    new User
                    {
                        Email = "admin@example.com",
                        Password = "123456",
                        IsAdmin = true,
                    }
                );

                context.Dlls.AddRange(
                    new Dll
                    {
                        Name = "OnlineVideos.Sites.Youtube",
                        LastUpdated = new DateTime(2023, 1, 1),
                        MD5 = "aaaa",
                        OwnerId = "admin@example.com"
                    },
                    new Dll
                    {
                        Name = "OnlineVideos.Sites.BBCiPlayer",
                        LastUpdated = new DateTime(2022, 6, 6),
                        MD5 = "bbbb",
                        OwnerId = "user@example.com"
                    }
                );

                context.Reports.AddRange(
                    new Report
                    {
                        SiteName = "YouTube",
                        Type = ReportType.Broken,
                        Message = "broken",
                        Date = new DateTime(2023, 1, 1)
                    },
                    new Report
                    {
                        SiteName = "BBC iPlayer",
                        Type = ReportType.Fixed,
                        Message = "fixed",
                        Date = new DateTime(2023, 6, 6)
                    }
                );

                context.Sites.AddRange(
                    new Site
                    {
                        Name = "YouTube",
                        OwnerId = "admin@example.com",
                        DllId = "OnlineVideos.Sites.Youtube",
                        Description = "Largest video sharing site on the web. Available in any country.",
                        Language = "en",
                        LastUpdated = DateTime.Parse("2022-03-31T23:05:13.8981405+01:00"),
                        IsAdult = false,
                        XML = @"<Site name=""YouTube"" util=""YouTubeV3"" agecheck=""false"" enabled=""true"" lang=""en"">
      <Description>Largest video sharing site on the web. Available in any country.</Description>
    </Site>"
                    },
                    new Site
                    {
                        Name = "BBC iPlayer",
                        OwnerId = "user@example.com",
                        DllId = "OnlineVideos.Sites.BBCiPlayer",
                        Description = "The BBC iPlayer allows to catch up on the last 7 days of BBC TV (only available in the UK).",
                        Language = "en",
                        LastUpdated = DateTime.Parse("2023-03-31T23:05:13.8981405+01:00"),
                        IsAdult = false,
                        XML = @"<Site name=""BBC iPlayer"" util=""BBCiPlayer"" agecheck=""false"" enabled=""true"" lang=""en"" lastUpdated=""2023-03-31T23:05:13.8981405+01:00"">
      <Description>The BBC iPlayer allows to catch up on the last 7 days of BBC TV (only available in the UK).</Description>
      <Configuration>
        <item key=""tvGuideFormatString""><![CDATA[Now: <nowtitle> - <nowstart> - <nowend><newline>Next: <nexttitle> - <nextstart> - <nextend><newline><nowdescription>]]></item>
      </Configuration>
      <Categories>
        <Category xsi:type=""RssLink"" name=""Most Popular"">https://www.bbc.co.uk/iplayer/most-popular</Category>
        <Category xsi:type=""RssLink"" name=""Box Sets"">https://www.bbc.co.uk/iplayer/group/p05pn9jr</Category>
        <Category xsi:type=""RssLink"" name=""A - Z"">https://www.bbc.co.uk/iplayer/a-z/</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01n4zyb.jpg"" name=""Arts"">https://www.bbc.co.uk/iplayer/categories/arts/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p02984mp.jpg"" name=""CBBC"">https://www.bbc.co.uk/iplayer/categories/cbbc/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p027dlmb.jpg"" name=""CBeebies"">https://www.bbc.co.uk/iplayer/categories/cbeebies/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p020dl2m.jpg"" name=""Comedy"">https://www.bbc.co.uk/iplayer/categories/comedy/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01l8l4c.jpg"" name=""Documentaries"">https://www.bbc.co.uk/iplayer/categories/documentaries/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p0295f60.jpg"" name=""Drama &amp; Soaps"">https://www.bbc.co.uk/iplayer/categories/drama-and-soaps/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01tlxfz.jpg"" name=""Entertainment"">https://www.bbc.co.uk/iplayer/categories/entertainment/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01h6zpj.jpg"" name=""Films"">https://www.bbc.co.uk/iplayer/categories/films/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p023w4qg.jpg"" name=""Food"">https://www.bbc.co.uk/iplayer/categories/food/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01jykx1.jpg"" name=""History"">https://www.bbc.co.uk/iplayer/categories/history/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01zxkgz.jpg"" name=""Lifestyle"">https://www.bbc.co.uk/iplayer/categories/lifestyle/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01lccbf.jpg"" name=""Music"">https://www.bbc.co.uk/iplayer/categories/music/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p029fqtd.jpg"" name=""News"">https://www.bbc.co.uk/iplayer/categories/news/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p027s8wj.jpg"" name=""Science &amp; Nature"">https://www.bbc.co.uk/iplayer/categories/science-and-nature/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p024q1kt.jpg"" name=""Sport"">https://www.bbc.co.uk/iplayer/categories/sport/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2002.png"" name=""BBC One"">https://www.bbc.co.uk/bbcone/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2006.png"" name=""BBC Two"">https://www.bbc.co.uk/bbctwo/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://upload.wikimedia.org/wikipedia/en/thumb/d/dd/BBC_Three_logo.svg/100px-BBC_Three_logo.svg.png"" name=""BBC Three"">https://www.bbc.co.uk/tv/bbcthree/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2018.png"" name=""BBC Four"">https://www.bbc.co.uk/bbcfour/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2011.png"" name=""BBC News"">https://www.bbc.co.uk/tv/bbcnews/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2072.png"" name=""BBC Parliament"">https://www.bbc.co.uk/tv/bbcparliament/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2012.png"" name=""BBC ALBA"">https://www.bbc.co.uk/tv/bbcalba/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p0276wg9.jpg"" name=""Audio Described"">https://www.bbc.co.uk/iplayer/categories/audio-described/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p01x0kbv.jpg"" name=""Signed"">https://www.bbc.co.uk/iplayer/categories/signed/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p027lsl4.jpg"" name=""Northern Ireland"">https://www.bbc.co.uk/iplayer/categories/northern-ireland/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p025h74g.jpg"" name=""Scotland"">https://www.bbc.co.uk/iplayer/categories/scotland/a-z</Category>
        <Category xsi:type=""RssLink"" thumb=""http://ichef.bbci.co.uk/images/ic/406x228/p0295zvd.jpg"" name=""Wales"">https://www.bbc.co.uk/iplayer/categories/wales/a-z</Category>
        <Category xsi:type=""Group"" name=""Live TV"">
          <Channels>
            <Channel name=""BBC One"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2002.png"">bbc_one_hd?guideid=94</Channel>
            <Channel name=""BBC Two"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2006.png"">bbc_two_hd?guideid=105</Channel>
            <Channel name=""BBC Four"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2018.png"">bbc_four_hd?guideid=47</Channel>
            <Channel name=""CBBC"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2020.png"">cbbc_hd?guideid=482</Channel>
            <Channel name=""CBeebies"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2019.png"">cbeebies_hd?guideid=483</Channel>
            <Channel name=""BBC Scotland"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb1138.png"">bbc_scotland_hd</Channel>
            <Channel name=""BBC News"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2011.png"">bbc_news24?guideid=48</Channel>
            <Channel name=""BBC Parliament"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2072.png"">bbc_parliament?guideid=49</Channel>
            <Channel name=""BBC ALBA"" thumb=""https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/256/256/skychb2012.png"">bbc_alba?guideid=2098</Channel>
          </Channels>
        </Category>
      </Categories>
    </Site>",
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
