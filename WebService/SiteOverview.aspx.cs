using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OnlineVideos.WebService.Database;
using System.Globalization;

namespace OnlineVideos.WebService
{
    public partial class SiteOverview : System.Web.UI.Page
    {
        SiteState? CurrentSiteStateFilter
        {
            get { return (SiteState?)ViewState["CurrentSiteStateFilter"]; }
            set { ViewState["CurrentSiteStateFilter"] = value; }
        }

        string CurrentSortingProperty
        {
            get { return (string)ViewState["CurrentSortingProperty"] ?? "LastUpdated"; }
            set { ViewState["CurrentSortingProperty"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (dc.DatabaseExists())
                    {
                        txtNumSitesTotal.Text = dc.Site.Count().ToString();
                        txtNumReportedSites.Text = dc.Site.Count(s => s.State == SiteState.Reported).ToString();
                        txtNumBrokenSites.Text = dc.Site.Count(s => s.State == SiteState.Broken).ToString();

                        BindGrid(dc);
                    }
                }
            }
        }

        protected void siteOverview_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string creator = e.Row.Cells[3].Text;
                creator = creator.Substring(0, creator.IndexOf('@')).Replace('.', ' ').Replace('_', ' ');
                e.Row.Cells[3].Text = creator;
                switch ((e.Row.DataItem as Site).State)
                {
                    case SiteState.Reported: e.Row.Cells[2].CssClass = "reportedCell"; break;
                    case SiteState.Broken: e.Row.Cells[2].CssClass = "brokenCell"; break;
                }
            }
        }

        protected void siteOverview_Sorting(object sender, GridViewSortEventArgs e)
        {
            CurrentSortingProperty = e.SortExpression;
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                if (dc.DatabaseExists())
                {
                    BindGrid(dc);
                }
            }
        }

        void BindGrid(OnlineVideosDataContext dc)
        {
            var query = from a in dc.Site select new { Description = a.Description, Language = a.Language, IsAdult = a.IsAdult, LastUpdated = a.LastUpdated, Name = a.Name, State = a.State, Owner_FK = a.Owner_FK, RequiredDll = a.RequiredDll, ReportCount = (uint)dc.Report.Count(r => r.Site_FK == a.Name) };
            switch (CurrentSortingProperty)
            {
                case "Name": query = query.OrderBy(s => s.Name); break;
                case "Owner_FK": query = query.OrderBy(s => s.Owner_FK); break;
                case "Language": query = query.OrderBy(s => s.Language); break;
                case "LastUpdated": query = query.OrderByDescending(s => s.LastUpdated); break;
            }
            if (CurrentSiteStateFilter != null)
            {
                switch (CurrentSiteStateFilter)
                {
                    case SiteState.Reported: query = query.Where(s => s.State == SiteState.Reported); break;
                    case SiteState.Broken: query = query.Where(s => s.State == SiteState.Broken); break;
                }
            }
            siteOverview.DataSource = (List<Site>)query.ToList().ToNonAnonymousList(typeof(Site));
            siteOverview.DataBind();
        }

        protected string LanguageName(string aLang)
        {
            string name = aLang;
            try
            {
                name = aLang != "--" ? System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag(aLang).DisplayName : "Global";
            }
            catch
            {
                var temp = System.Globalization.CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(
                    ci => ci.IetfLanguageTag == aLang || ci.ThreeLetterISOLanguageName == aLang || ci.TwoLetterISOLanguageName == aLang || ci.ThreeLetterWindowsLanguageName == aLang);
                if (temp != null)
                {
                    name = temp.DisplayName;
                }
            }
            return name;
        }

        protected void btnFilterNone_Click(object sender, EventArgs e)
        {
            CurrentSiteStateFilter = null;
            btnFilterNone.Font.Bold = true; txtNumSitesTotal.Font.Bold = true;
            btnFilterReported.Font.Bold = false; txtNumReportedSites.Font.Bold = false;
            btnFilterBroken.Font.Bold = false; txtNumBrokenSites.Font.Bold = false;
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                if (dc.DatabaseExists())
                {
                    BindGrid(dc);
                }
            }
        }

        protected void btnFilterReported_Click(object sender, EventArgs e)
        {
            CurrentSiteStateFilter = SiteState.Reported;
            btnFilterNone.Font.Bold = false; txtNumSitesTotal.Font.Bold = false;
            btnFilterReported.Font.Bold = true; txtNumReportedSites.Font.Bold = true;
            btnFilterBroken.Font.Bold = false; txtNumBrokenSites.Font.Bold = false;
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                if (dc.DatabaseExists())
                {
                    BindGrid(dc);
                }
            }
        }

        protected void btnFilterBroken_Click(object sender, EventArgs e)
        {
            CurrentSiteStateFilter = SiteState.Broken;
            btnFilterNone.Font.Bold = false; txtNumSitesTotal.Font.Bold = false;
            btnFilterReported.Font.Bold = false; txtNumReportedSites.Font.Bold = false;
            btnFilterBroken.Font.Bold = true; txtNumBrokenSites.Font.Bold = true;
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                if (dc.DatabaseExists())
                {
                    BindGrid(dc);
                }
            }
        }
    }
}

namespace OnlineVideos.WebService.Database
{
	public partial class Site
	{
		public uint ReportCount { get; set; }
	}
}