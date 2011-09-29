using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OnlineVideos.WebService.Database;

namespace OnlineVideos.WebService
{
	public partial class Reports : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
				{
					if (dc.DatabaseExists())
					{
						var reportsQuery = from a in dc.Report where a.Site_FK == this.Request.Params["site"] select new { Message = a.Message, Type = a.Type, Date = a.Date, };
						reportsQuery = reportsQuery.OrderByDescending(s => s.Date);
						reports.DataSource = (List<Report>)reportsQuery.ToList().ToNonAnonymousList(typeof(Report));
						reports.DataBind();
					}
				}
			}

		}

		protected void reports_RowDataBound(object sender, GridViewRowEventArgs e)
		{
		}
	}
}