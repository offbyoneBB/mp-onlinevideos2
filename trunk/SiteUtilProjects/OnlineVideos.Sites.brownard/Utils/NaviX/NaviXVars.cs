using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXVars
    {
        Hashtable defaults = null;
        Hashtable vars = null;

        public NaviXVars()
        {
            defaults = initDefaults();
            vars = initDefaults();
        }

        public string this[string key]
        {
            get
            {
                string res = vars[key] as string;
                return res != null ? res : "";
            }
            set
            {
                if (value == null)
                    value = "";
                vars[key] = value;
            }
        }

        public void Reset(bool scrapeOnly = false)
        {
            if (scrapeOnly)
            {
                foreach (string ke in new string[] { "s_method", "s_action", "s_agent", "s_referer", "s_cookie", "s_postdata" })
                    vars[ke] = defaults[ke];
            }
            else
            {
                foreach (string ke in defaults.Keys)
                    vars[ke] = defaults[ke];
            }
        }

        Hashtable initDefaults()
        {
            return new Hashtable()
            {
                {"htmRaw", ""},
                {"s_url", ""},
                {"regex", ""},
                {"s_method", "get"},
                {"s_action", "read"},
                {"s_agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.4) Gecko/2008102920 Firefox/3.0.4"},
                {"s_referer", ""},
                {"s_cookie", ""},
                {"s_postdata", ""},
                {"url", ""},
                {"swfplayer", ""},
                {"playpath", ""},
                {"agent", ""},
                {"pageurl", ""},
                {"app", ""},
                {"swfVfy", ""},
                {"nookie_expires", "0"}
            };
        }
    }
}
