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
        public override Entities.EventResult PerformLogin(string username, string password)
        {
            ShowLoading();
            Url = "about:blank";
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        private string theUrl = "dummy";
        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            ShowLoading();
            theUrl = videoToPlay;
            Url = videoToPlay;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override void OnAction(string actionEnumName)
        {
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

        private bool isPausing = false;
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

            if (Url != "about:blank")
            {
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Tick += (object sender, EventArgs e) =>
                {
                    HideLoading();
                    timer.Stop();
                    timer.Dispose();
                    InvokeScript(Properties.Resources.Katsomo + "setTimeout(\"myZoom()\", 500);");
                };
                timer.Interval = 3500;
                timer.Start();
            }
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }
    }
}
