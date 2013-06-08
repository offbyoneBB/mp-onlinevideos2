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
			Title = this.Request.Params["site"];
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
                        reports.ShowFooter = User.Identity.Name == site.Owner_FK || User.IsInRole("admin");

                        reports.Columns[reports.Columns.Count - 1].Visible = User.Identity.Name == site.Owner_FK || User.IsInRole("admin");
                        var reportsQuery = from a in dc.Report where a.Site_FK == this.Request.Params["site"] select new { Message = a.Message, Type = a.Type, Date = a.Date, };
                        reportsQuery = reportsQuery.OrderByDescending(s => s.Date);
						var result = (List<Report>)reportsQuery.ToList().ToNonAnonymousList(typeof(Report));
						if (result.Count == 0) result.Add(null); // hack to make ASP.NET < 4 show header and footer when empty data
						reports.DataSource = result;
                        reports.DataBind();
                    }
                }
            }
        }

        protected void reports_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var row = (GridViewRow)((Control)e.CommandSource).Parent.Parent;
            string sitename = Request.Params["site"];

            if (e.CommandName == "DeleteReport")
            {
                ReportType type = (ReportType)Enum.Parse(typeof(ReportType), (row.Cells[0].Controls[1] as Label).Text);
                DateTime date = DateTime.ParseExact((row.Cells[1].Controls[1] as Label).Text, "g", System.Threading.Thread.CurrentThread.CurrentCulture);
                string message = HttpUtility.HtmlDecode((row.Cells[2].Controls[1] as Label).Text);

                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    var rep = dc.Report.FirstOrDefault(r => r.Message == message && (r.Date - date).Minutes <= 1 && r.Type == type && r.Site_FK == sitename);
                    if (rep != null)
                    {
                        dc.Report.DeleteOnSubmit(rep);
                        dc.SubmitChanges();
                        
                        // now get last report for this site and check with last update to set the new site state
                        Site site = dc.Site.First(s => s.Name == sitename);
                        var latestReport = dc.Report.Where(r => r.Site_FK == sitename).OrderByDescending(r => r.Date).FirstOrDefault();
                        if (latestReport == null)
                        {
                            site.State = SiteState.Working;
                        }
                        else
                        {
                            if (site.LastUpdated > latestReport.Date)
                            {
                                site.State = SiteState.Working;
                            }
                            else
                            {
                                if (latestReport.Type == ReportType.Broken && site.State == SiteState.Working) site.State = SiteState.Reported;
                                else if (latestReport.Type == ReportType.ConfirmedBroken) site.State = SiteState.Broken;
                                else if (latestReport.Type == ReportType.RejectedBroken || latestReport.Type == ReportType.Fixed || latestReport.Type == ReportType.Suggestion) site.State = SiteState.Working;
                            }
                        }
                        dc.SubmitChanges();

                        BindGrid();
                    }
                }
            }
            else if (e.CommandName == "AddReport")
            {
                ReportType type = (ReportType)Enum.Parse(typeof(ReportType), (row.FindControl("ddType") as DropDownList).SelectedValue);
                string message = (row.FindControl("tbxNewMessage") as TextBox).Text;
                if (!string.IsNullOrEmpty(message))
                {
                    using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                    {
                        Report report = new Report()
                        {
                            Date = DateTime.Now,
                            Message = message,
                            Type = type,
                            Site_FK = sitename
                        };
                        dc.Report.InsertOnSubmit(report);
                        
                        // set new site state
                        Site site = dc.Site.First(s => s.Name == sitename);
                        switch (type)
                        {
                            case ReportType.Broken:
                                if (site.State == SiteState.Working) site.State = SiteState.Reported;
                                break;
                            case ReportType.ConfirmedBroken:
                                site.State = SiteState.Broken;
                                break;
                            case ReportType.RejectedBroken:
                            case ReportType.Fixed:
                                site.State = SiteState.Working;
                                break;
                        }

                        dc.SubmitChanges();

                        BindGrid();
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
				try {
					System.IO.File.Delete(Server.MapPath("~/Icons/") + this.Request.Params["site"] + ".png");
				} catch { }
				try {
					System.IO.File.Delete(Server.MapPath("~/Banners/") + this.Request.Params["site"] + ".png");
				} catch { }
                Response.Redirect(ResolveUrl("~/SiteOverview.aspx"));
            }
        }
	}
}