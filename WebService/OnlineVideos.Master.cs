using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace OnlineVideos.WebService
{
	public partial class OnlineVideos : System.Web.UI.MasterPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
            var site = Request.Params["site"];
            if (!string.IsNullOrEmpty(site))
            {
                lblHeader.Text = string.Format("OnlineVideos - {0}", site);
                if (File.Exists(Server.MapPath(string.Format("~/Icons/{0}.png", site))))
                {
                    imgLinkIcon.ImageUrl = string.Format("~/Icons/{0}.png", site);
                }
            }
		}
	}
}