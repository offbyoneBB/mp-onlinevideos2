using OnlineVideos.Helpers;
using OnlineVideos.Sites.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class DplayConnector : BrowserUtilConnector
    {
        private string loginUrl;
        bool login = true;
        bool clicked = false;
        bool maximized = false;
        string username = null;
        string password = null;
        bool _showLoading = true;

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
            _showLoading = username.Contains("SHOWLOADING");
            username = username.Replace("SHOWLOADING", string.Empty);
            if (_showLoading) ShowLoading();
            //Need to position the mouse, otherwise automation can fail later on...
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 20, Browser.Location.Y + 200);
            Cursor.Hide();
            Application.DoEvents();
            string[] userStrings = username.Split('¥');
            this.username = userStrings[0];
            this.password = password;
            if (HaveCredentials)
                loginUrl = userStrings[1];
            else
                loginUrl = "about:blank";
            Url = loginUrl;
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

        public override EventResult Play()
        {
            return PlayPause();

        }

        public override EventResult Pause()
        {
            return PlayPause();
        }

        public EventResult PlayPause()
        {
            if (maximized)
            {
                System.Windows.Forms.SendKeys.Send(" ");
            }
            return EventResult.Complete();
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
                    InvokeScript("document.getElementById('login-email').value = '" + username + "';");
                    InvokeScript("document.getElementById('login-password').value = '" + password + "';");
                    InvokeScript("document.getElementById('remember-me-checkbox').checked = false;");
                    InvokeScript("setTimeout(\"document.getElementsByClassName('button-submit')[0].click()\", 2000);");
                    clicked = true;
                }
            }
            else
            {
                InvokeScript("setTimeout(\"document.getElementsByClassName('play-button')[0].click()\", 2000);");
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Tick += (object sender, EventArgs e) =>
                {
                    if (_showLoading) HideLoading();
                    if (!maximized)
                    {
                        maximized = true;
                        //Workaround for keeping maximized flashplayer on top
                        Browser.FindForm().Activated += FormActivated;
                        Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 20, Browser.Location.Y + 200);
                        Application.DoEvents();
                        Thread.Sleep(1000);
                        //Click only once
                        CursorHelper.DoLeftMouseClick();
                        Application.DoEvents();
                        Thread.Sleep(500);
                    }
                    System.Windows.Forms.SendKeys.Send("f");
                    timer.Stop();
                    timer.Dispose();
                };

                timer.Interval = 4500;
                timer.Start();
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
