using MediaPortal.UI.Presentation.DataObjects;

namespace OnlineVideos.MediaPortal2
{
    public class ReportViewModel : ListItem
    {
        public OnlineVideosWebservice.Report Report { get; protected set; }

        public ReportViewModel(OnlineVideosWebservice.Report report)
        {
            Report = report;
        }
    }
}
