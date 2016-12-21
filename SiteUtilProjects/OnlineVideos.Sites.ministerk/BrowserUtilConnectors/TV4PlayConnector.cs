using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using System.Threading;


namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class TV4PlayConnector : BrowserUtilConnector
    {
        private string username = null;
        private string password = null;
        bool login = true;
        bool clicked = false;
        bool maximized = false;
        bool startTimer = true;
        bool showLoading = true;
        bool isPremium = false;

        private const string loginUrl = "https://www.tv4play.se/sso/asset_splash/session/new?id={0}&partner=tv4play.se";

        private bool HaveCredentials
        {
            get { return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password); }
        }

        public override void OnClosing()
        {
            if (maximized)
                Browser.FindForm().Activated -= FormActivated;
            base.OnClosing();
        }
        public override EventResult PerformLogin(string username, string password)
        {
            Cursor.Hide();
            Application.DoEvents();
            showLoading = username.Contains("SHOWLOADING");
            username = username.Replace("SHOWLOADING", string.Empty);
            isPremium = username.Contains("PREMIUM");
            username = username.Replace("PREMIUM", string.Empty);
            string[] userStrings = username.Split('¥');
            username = userStrings[0];
            string videoId = userStrings[1];
            if (showLoading) ShowLoading();
            this.username = username;
            this.password = password;
            if (!HaveCredentials)
            {
                ProcessComplete.Finished = true;
                ProcessComplete.Success = false;
                return EventResult.Error("No login credantials");
            }
            Url = string.Format(loginUrl, videoId);
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            Url = videoToPlay;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override void OnAction(string actionEnumName)
        {
            if (actionEnumName == "ACTION_MOVE_LEFT")
            {
                SendKeyToBrowser("{LEFT}");
            }
            if (actionEnumName == "ACTION_MOVE_RIGHT")
            {
                SendKeyToBrowser("{RIGHT}");
            }
        }

        private void SendKeyToBrowser(string key)
        {
            /*
            if (maximized && isPremium)
            {

                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 135, Browser.FindForm().Bottom - 100);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 135, Browser.FindForm().Bottom - 35);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                System.Windows.Forms.SendKeys.Send(key);
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 135, Browser.FindForm().Bottom - 100);
                Application.DoEvents();
                InvokeScript("document.getElementById('player').focus();"); //Prevent play/pause problem
            }*/
        }

        private EventResult PlayPause()
        {
            if (maximized)
            {
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 100, Browser.FindForm().Bottom - 100);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 35, Browser.FindForm().Bottom - 35);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 100, Browser.FindForm().Bottom - 100);
                Application.DoEvents();
                InvokeScript("document.getElementById('player').focus();"); //Prevent play/pause problem
                Application.DoEvents();
            }
            return EventResult.Complete();

        }

        public override EventResult Play()
        {
            return PlayPause();
        }

        public override EventResult Pause()
        {
            return PlayPause();
        }

        bool refreshed = false;
        public override EventResult BrowserDocumentComplete()
        {
            MessageHandler.Info("Url: {0}, login: {1}, clicked: {2}, maximized: {3}", Url, login.ToString(), clicked.ToString(), maximized.ToString());
            if (login)
            {
                if (refreshed)
                {
                    login = false;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                }
                else
                {
                    if (!clicked)
                    {
                        string js = " function doLogin() { ";
                        js += "document.getElementsByName('username')[0].value = '" + username + "'; ";
                        js += "document.getElementsByName('password')[0].value = '" + password + "'; ";
                        js += "document.getElementsByClassName('btn')[0].click(); }; ";
                        js += "setTimeout(\"doLogin()\", 1500); ";
                        InvokeScript(js);
                        clicked = true;
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Tick += (object sender, EventArgs e) =>
                        {
                            refreshed = true;
                            timer.Stop();
                            timer.Dispose();
                            //Browser.Refresh();
                            Url = "https://www.tv4play.se/";
                        };
                        timer.Interval = 3000;
                        timer.Start();
                    }
                }
            }
            else
            {
                
                InvokeScript("setTimeout(\"document.getElementById('player').setAttribute('style', 'position: fixed; z-index: 11000; top: 0px; left: 0px; width: 100%; height: 100%')\", 1000);");
                if (startTimer)
                {
                    startTimer = false;
                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Tick += (object sender, EventArgs e) =>
                    {
                        if (showLoading) HideLoading();
                        timer.Stop();
                        timer.Dispose();
                    };

                    timer.Interval = 2250;
                    timer.Start();

                    System.Windows.Forms.Timer maxTimer = new System.Windows.Forms.Timer();
                    maxTimer.Tick += (object sender, EventArgs e) =>
                    {
                        Cursor.Position = new System.Drawing.Point(Browser.FindForm().Right - 100, Browser.FindForm().Bottom - 100);
                        Application.DoEvents();
                        Cursor.Position = new System.Drawing.Point(Browser.FindForm().Right - 35, Browser.FindForm().Bottom - 35);
                        Application.DoEvents();
                        CursorHelper.DoLeftMouseClick();
                        Application.DoEvents();
                        Cursor.Position = new System.Drawing.Point(Browser.FindForm().Right - 100, Browser.FindForm().Bottom - 100);
                        Application.DoEvents();
                        //Workaround for keeping maximized flashplayer on top
                        Browser.FindForm().Activated += FormActivated;
                        InvokeScript("document.getElementById('player').focus();"); //Prevent play/pause problem
                        maximized = true;
                        maxTimer.Stop();
                        maxTimer.Dispose();
                    };
                    maxTimer.Interval = isPremium ? 15000 : 60000;
                    maxTimer.Start();
                }
                ProcessComplete.Finished = true;
                ProcessComplete.Success = true;
            }
            return EventResult.Complete();
        }
        private void FormActivated(object sender, EventArgs e)
        {
            this.Browser.FindForm().SendToBack();
        }
    }
}
