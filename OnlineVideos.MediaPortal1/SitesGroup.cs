using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MediaPortal1
{
    public class SitesGroup
    {
        public SitesGroup()
        {
            Name = "new";
            Sites = new BindingList<string>();
        }
        public string Name { get; set; }
        public string Thumbnail { get; set; }
        public BindingList<string> Sites { get; set; }
    }
}
