using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using System.Threading;
using OnlineVideos.Helpers;
using System.Diagnostics;
using OnlineVideos.Sites.Utils;
using System.Xml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class HboNordicConnector : BrowserUtilConnector
    {

        protected enum State
        {
            None,
            Login,
            Start,
            HideSpinner,
            Playing
        }

        protected bool _isUsingMouse = false;
        protected State _currentState = State.None;
        private string _password;
        private string _username;
        private string _redirectUrl;
        private bool _removeFormWatchlist = false;
        private int _currentSubtitle = 0;
        private int[] _subtitleOffsets = { 72, 112, 152, 192, 232 };
        private int _xOffset = 157;
        private int _yOffset = 27;

        private int SubtitleOffset
        {
            get
            {
                int offset = _currentSubtitle;
                _currentSubtitle++;
                _currentSubtitle = _currentSubtitle % 5;
                return _subtitleOffsets[offset];
            }
        }

        public override void OnClosing()
        {
            base.OnClosing();
        }

        string _loginUrl;
        public override EventResult PerformLogin(string username, string password)
        {
            ShowLoading();
            string[] parts = username.Split('|');
            _password = password;
            _username = parts[0];
            _redirectUrl = "about:blank";
            _loginUrl = parts[1];
            Url = _loginUrl;
            _currentState = State.Login;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            string[] parts = videoToPlay.Split('|');
            bool.TryParse(parts[1], out _removeFormWatchlist);
            Url = parts[0];
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            _currentState = State.Start;
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

        private EventResult PlayPause()
        {
            if (_currentState != State.Playing || _isUsingMouse || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isUsingMouse = true;
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Right - _xOffset, Browser.FindForm().Bottom - 350);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            //To show sub -language
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Right - _xOffset, Browser.FindForm().Bottom - _yOffset);
            Application.DoEvents();
            _isUsingMouse = false;
            return EventResult.Complete();
        }

        public override void OnAction(string actionEnumName)
        {
            if (_currentState != State.Playing || _isUsingMouse || Browser.Document == null || Browser.Document.Body == null) return;

            if (actionEnumName == "ACTION_NEXT_SUBTITLE" || actionEnumName == "ACTION_SHOW_INFO")
            {
                _isUsingMouse = true;
                int w = Browser.FindForm().Right;
                int h = Browser.FindForm().Bottom;
                int subPosX = w - _xOffset;
                int subPosY = h - _yOffset;
                Cursor.Position = new System.Drawing.Point(subPosX, h - 350);
                Application.DoEvents();
                int moveOffset = 2;
                //Move around the mouse...
                for (int i = 0; i < 20; i++)
                {
                    moveOffset *= -1;
                    Cursor.Position = new System.Drawing.Point(subPosX, subPosY + moveOffset);
                    Application.DoEvents();
                    Thread.Sleep(50);
                }
                Cursor.Position = new System.Drawing.Point(subPosX, h - SubtitleOffset);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                _isUsingMouse = false;
            }
        }

        public override EventResult BrowserDocumentComplete()
        {
            switch (_currentState)
            {
                case State.Login:
                    if (Url == _redirectUrl)
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        _currentState = State.None;
                    }
                    else if (Url.ToLower().Contains("/account"))
                    {
                        InvokeScript(Properties.Resources.HboNordicJs + "setTimeout(\"__login('" + _username + "','" + _password + "','" + _redirectUrl + "' )\", 1000); ");
                    }
                    break;
                case State.Start:
                    if (Url.Contains("/watchlist"))
                    {
                        InvokeScript(Properties.Resources.HboNordicJs + "setTimeout(\"__startWatch()\", 1000);");
                        if (_removeFormWatchlist)
                            InvokeScript("setTimeout(\"__removeFromWatchlist()\", 2000);");
                        _currentState = State.HideSpinner;
                    }
                    break;
                case State.HideSpinner:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    _currentState = State.Playing;
                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Tick += (object sender, EventArgs e) =>
                    {
                        HideLoading();
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Interval = 1500;
                    timer.Start();
                    break;
                case State.Playing:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                default:
                    break;
            }
            return EventResult.Complete();
        }
    }
}