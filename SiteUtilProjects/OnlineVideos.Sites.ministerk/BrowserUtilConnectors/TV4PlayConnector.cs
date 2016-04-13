using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;


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

        private const string loginUrl = "https://www.tv4play.se/session/new";

        private bool HaveCredentials
        {
            get { return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password); }
        }

        public override EventResult PerformLogin(string username, string password)
        {
            ShowLoading();
            this.username = username;
            this.password = password;
            if (HaveCredentials)
                Url = loginUrl;
            else
                Url = "about:blank";
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
            if (maximized)
            {

                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.Location.Y + 300);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                System.Windows.Forms.SendKeys.Send(key);
            }
        }

        private EventResult PlayPause()
        {
            if (maximized)
            {
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.Location.Y + 200);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.Location.Y + 300);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                InvokeScript("document.getElementById('player').focus();"); //Prevent play/pause problem
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

        public override EventResult BrowserDocumentComplete()
        {
            MessageHandler.Info("Url: {0}, login: {1}, clicked: {2}, maximized: {3}", Url, login.ToString(), clicked.ToString(), maximized.ToString());
            if (login)
            {
                if (Url == "about:blank")
                {
                    login = false;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;

                }
                else if (clicked && Url != loginUrl)
                {
                    login = false;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                }
                else
                {
                    InvokeScript("document.getElementById('user-name').value = '" + username + "';");
                    InvokeScript("document.getElementById('password').value = '" + password + "';");
                    InvokeScript("document.getElementById('remember_me').checked = false;");
                    InvokeScript("setTimeout(\"document.getElementsByClassName('btn')[0].click()\", 2000);");
                    clicked = true;
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
                        HideLoading();
                        maximized = true;
                        timer.Stop();
                        timer.Dispose();
                    };

                    timer.Interval = 2250;
                    timer.Start();
                }
                ProcessComplete.Finished = true;
                ProcessComplete.Success = true;
            }
            return EventResult.Complete();
        }
    }
}
