using OnlineVideos.Sites.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites.Base
{
    /// <summary>
    /// Base class for any browser util
    /// Implementations of this class will be passed to the OnlineVideos.Sites.WebAutomation.BrowserHost 
    /// </summary>
    public abstract class BrowserUtilConnector
    {
        /// <summary>
        /// Was the last request complete?
        /// </summary>
        public AsyncWaitResult ProcessComplete { get; internal set; }

        /// <summary>
        /// Web browser instance
        /// </summary>
        public WebBrowser Browser { get; set; }

        /// <summary>
        /// Error Handler
        /// </summary>
        public ILog MessageHandler {get; private set;}

        /// <summary>
        /// Perform a login to the target website
        /// </summary>
        public abstract EventResult PerformLogin(string username, string password);

        /// <summary>
        /// Begin playback of the specified video
        /// The videoToPlay will be one of the parameters sent to the browserhost
        /// </summary>
        /// <param name="videoToPlay"></param>
        /// <returns></returns>
        public abstract EventResult PlayVideo(string videoToPlay);

        /// <summary>
        /// Play button was pressed, so resume playback
        /// </summary>
        /// <returns></returns>
        public abstract EventResult Play();

        /// <summary>
        /// Pause button was pressed
        /// </summary>
        /// <returns></returns>
        public abstract EventResult Pause();

        /// <summary>
        /// Method called when the DocumentComplete event is handled from the browser
        /// </summary>
        /// <returns></returns>
        public abstract EventResult BrowserDocumentComplete();

        /// <summary>
        /// Shortcut property for the url
        /// </summary>
        public string Url
        {
            get
            {
                return Browser.Url.ToString();
            }
            set
            {

                Browser.Navigate(value);
            }
        }

        /// <summary>
        /// Fired when the browser host is closing
        /// </summary>
        public virtual void OnClosing()
        { 
        }

        /// <summary>
        /// Allow implementations to handle actions which aren't handled by the browserhost
        /// Unfortunately, because of dependencies, we need to pass in the name of the action enumeration rather than the enum itself
        /// </summary>
        /// <param name="action"></param>
        public virtual void OnAction(string actionEnumName)
        { 
        
        }

        /// <summary>
        /// Constructor - attach to the web browser supplied
        /// </summary>
        /// <param name="browser"></param>
        public void Initialise (WebBrowser browser, ILog messageHandler)
        {
            ProcessComplete = new AsyncWaitResult();
            Browser = browser;
            MessageHandler = messageHandler;
            Browser.DocumentCompleted += Browser_DocumentCompleted;
        }

        /// <summary>
        /// Pass the document completed event to the child connectors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
           try
            {
                var result = BrowserDocumentComplete();

                if (result.Result != EventResultType.Complete)
                    throw new ApplicationException("Error processing " +  this.GetType().ToString() + " : " + result.ErrorMessage);
            }
            catch (Exception ex)
            {
                RedirectOnError(ex);
            }
        }

        /// <summary>
        /// Log an error, detach the event handler and redirect to blank page
        /// </summary>
        /// <param name="error"></param>
        private void RedirectOnError(Exception ex)
        {
            Browser.DocumentCompleted -= Browser_DocumentCompleted;
            if (MessageHandler != null) MessageHandler.Error(ex);
            Browser.Navigate("about:blank");
            ProcessComplete.Finished = true;
            ProcessComplete.Success = false;
        }

        /// <summary>
        /// Invoke javascript on the page
        /// </summary>
        /// <param name="scriptToRun"></param>
        public void InvokeScript(string scriptToRun)
        {
            Browser.Document.InvokeScript("execScript", new Object[] { scriptToRun, "JavaScript" });
        }

        /// <summary>
        /// Remove the specified files from temporary internet files
        /// </summary>
        /// <param name="fileNameStartsWith"></param>
        /// <param name="fileExtension"></param>
        public void RemoveFileFromTempInternetFiles(string fileNameStartsWith, string fileExtension)
        {
            WebBrowserHelper.ClearCache(fileNameStartsWith, fileExtension);
        }

        /// <summary>
        /// Poll ProcessComplete, waiting for the timeout (in seconds)
        /// Returns true if the process completed, otherwise we get false (a timeout)
        /// </summary>
        /// <param name="forceQuit"></param>
        /// <param name="pollTimeout"></param>
        public bool WaitForComplete(Func<bool> forceQuit, int pollTimeout = 20)
        {
            var end = DateTime.Now.AddSeconds(pollTimeout);

            while (!ProcessComplete.Finished && DateTime.Now < end && forceQuit != null && !forceQuit())
            {
                // Must use DoEvents otherwise the browser control doesn't work....
                System.Windows.Forms.Application.DoEvents();
            }

            var ended = DateTime.Now;

            return ended < end && ProcessComplete.Success;
        }
    }
}
