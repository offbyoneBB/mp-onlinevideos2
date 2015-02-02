namespace OnlineVideos.Hoster
{
    /// <summary>
    /// Hoster class for www.ginbig.com
    /// 
    /// It's basically the same as sharefiles4u.com, only on a different domain
    /// </summary>
    public class GinBig : ShareFiles4u
    {
        public override string GetHosterUrl()
        {
            return "ginbig.com";
        }
    }
}
