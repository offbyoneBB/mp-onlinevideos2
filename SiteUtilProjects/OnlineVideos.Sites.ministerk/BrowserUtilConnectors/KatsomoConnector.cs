using OnlineVideos.Sites.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.BrowserUtilConnectors 
{
    public class KatsomoConnector : BrowserUtilConnector
    {
        private bool isPausing = false;
        private bool showLoading = true;

        public override Entities.EventResult PerformLogin(string username, string password)
        {
            showLoading = username.Contains("SHOWLOADING");
            if (showLoading)
                ShowLoading();
            Url = "about:blank";
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            if (showLoading)
                ShowLoading();
            Url = videoToPlay;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override void OnAction(string actionEnumName)
        {
            MessageHandler.Info("KatsomoConnector. actionEnumName: {0}", actionEnumName);
            //Adding remote 0 (with my remote red button did not work...)
            if (actionEnumName == "ACTION_SHOW_GUI" || actionEnumName == "REMOTE_0" || actionEnumName == "ACTION_REMOTE_RED_BUTTON")
            {
                InvokeScriptAndMoveCursor("myToggleZoom();");
            }
            if (actionEnumName == "ACTION_MOVE_LEFT")
            {
                InvokeScriptAndMoveCursor("myBack();");
            }
            if (actionEnumName == "ACTION_MOVE_RIGHT")
            {
                InvokeScriptAndMoveCursor("myForward();");
            }
        }


        public override Entities.EventResult Play()
        {
            return PlayPause();
        }

        public override Entities.EventResult Pause()
        {
            return PlayPause();
        }

        private Entities.EventResult PlayPause()
        {
            if (isPausing)
                InvokeScriptAndMoveCursor("myPlay();");
            else
                InvokeScriptAndMoveCursor("myPause();");
            isPausing = !isPausing;
            return EventResult.Complete();
        }

        private void InvokeScriptAndMoveCursor(string js)
        {
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
            Application.DoEvents();
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
            Application.DoEvents();
            InvokeScript(js);
        }

        public override Entities.EventResult BrowserDocumentComplete()
        {
            MessageHandler.Info("KatsomoConnector - Url: {0}", Url);
            if (Url != "about:blank")
            {
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Tick += (object sender, EventArgs e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    InvokeScript(Properties.Resources.Katsomo);
                    InvokeScript("setTimeout(\"myZoom()\", 500);");
                    if (showLoading)
                        HideLoading();
                };
                timer.Interval = 4500;
                timer.Start();
            }
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }
    }
}
