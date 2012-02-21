namespace OnlineVideos.MediaPortal1
{
    internal class NavigationContextSwitch
    {
        public Sites.SiteUtilBase ReturnToUtil { get; set; }
        public Category ReturnToCategory { get; set; }
        public Sites.SiteUtilBase GoToUtil { get; set; }
        public Category GoToCategory { get; set; }
        public Category BridgeCategory { get; set; }
    }
}
