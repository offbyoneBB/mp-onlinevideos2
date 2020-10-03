using System.Diagnostics;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Entities;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class NPOStartConnector : BrowserUtilConnector
    {
        public override EventResult Pause()
        {
            Click();
            return EventResult.Complete();
        }

        public override EventResult Play()
        {
            Click();
            return EventResult.Complete();
        }

        private void Click()
        {
            var frm = Browser.FindForm();
            Cursor.Position = new System.Drawing.Point(frm.Location.X + frm.Width / 2, frm.Location.Y + frm.Height / 2);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
        }

        public override void OnClosing()
        {
            Process.GetCurrentProcess().Kill();
        }


        public override EventResult PlayVideo(string videoToPlay)
        {
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = videoToPlay;
            return EventResult.Complete();
        }

        public override EventResult PerformLogin(string username, string password)
        {
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }

        public override EventResult BrowserDocumentComplete()
        {
            Cursor.Hide();
            Application.DoEvents();
            Click();

            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }
    }
}
