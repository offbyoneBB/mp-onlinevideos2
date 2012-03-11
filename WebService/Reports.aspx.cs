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
                BindGrid();
			}
		}

        private void BindGrid()
        {
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                if (dc.DatabaseExists())
                {
                    var site = dc.Site.FirstOrDefault(s => s.Name == this.Request.Params["site"]);
                    if (site != null)
                    {
                        btnDeleteSite.Visible = 
                            (User.Identity.Name == site.Owner_FK && 
                            site.State == SiteState.Broken && 
                            (DateTime.Now - dc.Report.Where(r => r.Site_FK == this.Request.Params["site"] && r.Type == ReportType.ConfirmedBroken).OrderByDescending(r => r.Date).Select(r => r.Date).FirstOrDefault()).TotalDays > 10) 
                            || User.IsInRole("admin");

                        reports.Columns[reports.Columns.Count - 1].Visible = User.Identity.Name == site.Owner_FK || User.IsInRole("admin");
                        var reportsQuery = from a in dc.Report where a.Site_FK == this.Request.Params["site"] select new { Message = a.Message, Type = a.Type, Date = a.Date, };
                        reportsQuery = reportsQuery.OrderByDescending(s => s.Date);
                        reports.DataSource = (List<Report>)reportsQuery.ToList().ToNonAnonymousList(typeof(Report));
                        reports.DataBind();
                    }
                }
            }
        }

        protected void reports_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeleteReport")
            {
                ReportType type = (ReportType)Enum.Parse(typeof(ReportType), (sender as GridView).Rows[int.Parse(e.CommandArgument.ToString())].Cells[0].Text);
                string date = (sender as GridView).Rows[int.Parse(e.CommandArgument.ToString())].Cells[1].Text;
                string message = (sender as GridView).Rows[int.Parse(e.CommandArgument.ToString())].Cells[2].Text;

                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    var rep = dc.Report.FirstOrDefault(r => r.Message == HttpUtility.HtmlDecode(message) && r.Type == type && r.Site_FK == this.Request.Params["site"]);
                    if (rep != null)
                    {
                        dc.Report.DeleteOnSubmit(rep);
                        dc.SubmitChanges();
                        BindGrid();
                        linkOverview.Visible = true;
                    }
                }
            }
        }

        protected void btnDeleteSite_Click(object sender, EventArgs e)
        {
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                dc.Report.DeleteAllOnSubmit(dc.Report.Where(r => r.Site_FK == this.Request.Params["site"]));
                dc.Site.DeleteOnSubmit(dc.Site.FirstOrDefault(s => s.Name == this.Request.Params["site"]));
                dc.SubmitChanges();
                Response.Redirect(ResolveUrl("~/SiteOverview.aspx"));
            }
        }
	}
}