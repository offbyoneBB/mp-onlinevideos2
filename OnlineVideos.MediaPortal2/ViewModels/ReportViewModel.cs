using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

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
