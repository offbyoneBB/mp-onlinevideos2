using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OnlineVideos.WebService.Database;

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
                        var sites = from a in dc.Site select new { Description = a.Description, Language = a.Language, IsAdult = a.IsAdult, LastUpdated = a.LastUpdated, Name = a.Name, State = a.State, Owner_FK = a.Owner_FK, RequiredDll = a.RequiredDll };
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
                switch ((e.Row.DataItem as Site).State)
                {
                    case SiteState.Reported: e.Row.ForeColor = System.Drawing.Color.Yellow; break;
                    case SiteState.Broken: e.Row.ForeColor = System.Drawing.Color.Red; break;                    
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
    }
}
