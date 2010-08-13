using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Profile;

namespace OnlineVideos.MediaPortal1
{
    public class UserStore : IUserStore
    {
        public string GetValue(string key)
        {
            using (Settings settings = new MPSettings())
            {
                string value = settings.GetValueAsString(PluginConfiguration.CFG_SECTION, key, "NO_VALUE_FOUND");
                if (value != "NO_VALUE_FOUND") return value;
                else return null;
            }
        }

        public void SetValue(string key, string value)
        {
            using (Settings settings = new MPSettings())
            {
                settings.SetValue(PluginConfiguration.CFG_SECTION, key, value);
            }
        }
    }
}
