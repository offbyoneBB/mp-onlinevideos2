using System;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class upload180 : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "180upload.com";
        }
        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);

            //Grab content and decompress Dean Edwards compressor
            string packed = GetSubString(page, @"swfobject.js'></script>", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UnPack(packed);

            //Grab file url from decompresst content
            string res = GetSubString(unpacked, @"'file','", @"'");

            if (!String.IsNullOrEmpty(res))
            {
                return res;
            }
            return String.Empty;
        }
    }
}
