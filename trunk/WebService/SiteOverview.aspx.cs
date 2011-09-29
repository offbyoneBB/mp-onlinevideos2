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
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (dc.DatabaseExists())
                    {
						var sites = from a in dc.Site select new { Description = a.Description, Language = a.Language, IsAdult = a.IsAdult, LastUpdated = a.LastUpdated, Name = a.Name, State = a.State, Owner_FK = a.Owner_FK, RequiredDll = a.RequiredDll, ReportCount = (uint)dc.Report.Count(r => r.Site_FK == a.Name) };
                        sites = sites.OrderByDescending(s => s.LastUpdated);
						siteOverview.DataSource = (List<Site>)sites.ToList().ToNonAnonymousList(typeof(Site));
                        siteOverview.DataBind();
                    }
                }
            }
        }

        protected void siteOverview_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string creator = e.Row.Cells[2].Text;
                creator = creator.Substring(0, creator.IndexOf('@'));
                e.Row.Cells[2].Text = creator;
                e.Row.Cells[3].Text = LanguageName(e.Row.Cells[3].Text);
                switch ((e.Row.DataItem as Site).State)
                {
                    case SiteState.Reported: e.Row.Cells[1].BackColor = System.Drawing.Color.FromArgb(255, 240, 79); break;
                    case SiteState.Broken: e.Row.Cells[1].BackColor = System.Drawing.Color.Red; break;
                }
				if ((e.Row.DataItem as Site).ReportCount > 0)
				{
					//e.Row.Cells[1].te
				}
            }
        }

        protected void siteOverview_Sorting(object sender, GridViewSortEventArgs e)
        {
            using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
            {
                if (dc.DatabaseExists())
                {
                    var query = from a in dc.Site select new { Description = a.Description, Language = a.Language, IsAdult = a.IsAdult, LastUpdated = a.LastUpdated, Name = a.Name, State = a.State, Owner_FK = a.Owner_FK, RequiredDll = a.RequiredDll };
                    switch (e.SortExpression)
                    {
                        case "Name": query = query.OrderBy(s => s.Name); break;
                        case "Owner_FK": query = query.OrderBy(s => s.Owner_FK); break;
                        case "Language": query = query.OrderBy(s => s.Language); break;
                        case "LastUpdated": query = query.OrderByDescending(s => s.LastUpdated); break;
                    }
                    siteOverview.DataSource = (List<Site>)query.ToList().ToNonAnonymousList(typeof(Site));
                    siteOverview.DataBind();
                }
            }
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
    }
}

namespace OnlineVideos.WebService.Database
{
	public partial class Site
	{
		public uint ReportCount { get; set; }
	}
}