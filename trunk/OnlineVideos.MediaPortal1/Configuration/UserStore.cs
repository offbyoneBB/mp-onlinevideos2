using System;
using MediaPortal.Profile;
using OnlineVideos.Helpers;

namespace OnlineVideos.MediaPortal1
{
    public class UserStore : MarshalByRefObject, IUserStore
    {
        public string GetValue(string key, bool decrypt = false)
        {
            using (Settings settings = new MPSettings())
            {
                string value = settings.GetValueAsString(PluginConfiguration.CFG_SECTION, key, "NO_VALUE_FOUND");
                if (value != "NO_VALUE_FOUND") return decrypt ? EncryptionUtils.SymDecryptLocalPC(value) : value;
                else return null;
            }
        }

        public void SetValue(string key, string value, bool encrypt = false)
        {
            using (Settings settings = new MPSettings())
            {
                if (encrypt) value = EncryptionUtils.SymEncryptLocalPC(value);
                if (string.IsNullOrWhiteSpace(value))
                    settings.RemoveEntry(PluginConfiguration.CFG_SECTION, key);
                else
                    settings.SetValue(PluginConfiguration.CFG_SECTION, key, value);
            }
        }

		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion
	}
}
