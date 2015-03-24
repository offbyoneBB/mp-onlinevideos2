using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonBrowserSession : BrowserSessionBase
    {
        private Boolean _isLoggedIn;
        private long _lastLogin;
        private string _lastCulture;
        private string _userAgent = "Mozilla/5.0 (X11; U; Linux i686; en-US) AppleWebKit/533.4 (KHTML, like Gecko) Chrome/5.0.375.127 Large Screen Safari/533.4 GoogleTV/ 162671";

        public AmazonBrowserSession()
            : base()
        {
            Log.Info("New Browser session");
            _isLoggedIn = false;
            _lastLogin = 0;
            _lastCulture = "";
            userAgent = _userAgent;
        }

        public void Login(string username, string password, bool forceLogin = false)
        {
            string culture = Properties.Resources.Culture.ToString();
            long day = DateTime.Now.Ticks / TimeSpan.TicksPerDay;
            
            if (day > _lastLogin || culture != _lastCulture)
            {
                forceLogin = true;
            }

            if (!_isLoggedIn || forceLogin)
            {
                Log.Info("Login to Amazon " + Properties.Resources.AmazonRootUrl);
                _isLoggedIn = false;
                _lastCulture = culture;
                var loginDoc = Load(Properties.Resources.AmazonLoginUrl);
                var formElements = new FormElementCollection(loginDoc);
                string postUrl = loginDoc.GetElementbyId("ap_signin_form").Attributes["action"].Value;
                formElements["email"] = username;
                formElements["password"] = password;
                formElements["create"] = "0";
                string login = GetWebData(Properties.Resources.AmazonLoginUrl, formElements.AssemblePostPayload(), cc, null, null, false, false, userAgent);
                _isLoggedIn = true;
                _lastLogin = day;
                Log.Info("Login complete");
            }
        }

    }
}
