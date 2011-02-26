using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Cornerstone.Tools;
using OnlineVideos.Sites.Cornerstone.Tools;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{
    [ScraperNode("retrieve")]
    public class RetrieveNode : ScraperNode {
        #region Properties

        public string Url {
            get { return url; }
        } protected String url;

        public string File {
            get { return file; }
        } protected String file;

        public int MaxRetries {
            get { return maxRetries; }
        } protected int maxRetries;

        public Encoding Encoding {
            get { return encoding; }
        } protected Encoding encoding = null;

        public String UserAgent {
            get { return userAgent; }
        } protected String userAgent;

        public int Timeout {
            get { return timeout; }
        } protected int timeout;

        public int TimeoutIncrement {
            get { return timeoutIncrement; }
        } protected int timeoutIncrement;

        public bool AllowUnsafeHeader {
            get { return allowUnsafeHeader; }
        } protected bool allowUnsafeHeader;
        #endregion

        #region Methods

        public RetrieveNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {

            // Set default attribute valuess
            userAgent = "Mozilla/5.0 (Windows; U; MSIE 7.0; Windows NT 6.0; en-US)";
            allowUnsafeHeader = false;
            maxRetries = 5;
            timeout = 5000;
            timeoutIncrement = 2000;

            // Load attributes
            foreach (XmlAttribute attr in xmlNode.Attributes) {
                switch (attr.Name) {
                    case "url":
                        url = attr.Value;
                        break;
                    case "file":
                        file = attr.Value;
                        break;
                    case "useragent":
                        userAgent = attr.Value;
                        break;
                    case "allow_unsafe_header":
                        try { allowUnsafeHeader = bool.Parse(attr.Value); }
                        catch (Exception e) {
                            if (e.GetType() == typeof(ThreadAbortException))
                                throw e;
                        }
                        break;
                    case "encoding":
                        // grab encoding, if not specified it will try to set 
                        // the encoding using information from the response header.
                        try { encoding = Encoding.GetEncoding(attr.Value); }
                        catch (Exception e) {
                            if (e.GetType() == typeof(ThreadAbortException))
                                throw e;
                        }
                        break;
                    case "retries":
                        try { maxRetries = int.Parse(attr.Value); }
                        catch (Exception e) {
                            if (e.GetType() == typeof(ThreadAbortException))
                                throw e;
                        }
                        break;
                    case "timeout":
                        try { timeout = int.Parse(attr.Value); }
                        catch (Exception e) {
                            if (e.GetType() == typeof(ThreadAbortException))
                                throw e;
                        }
                        break;
                    case "timeout_increment":
                        try { timeoutIncrement = int.Parse(attr.Value); }
                        catch (Exception e) {
                            if (e.GetType() == typeof(ThreadAbortException))
                                throw e;                            
                        }
                        break;
                }
            }

            // Validate URL / FILE attribute
            if (url == null && file == null) {
                logger.Error("Missing URL or FILE attribute on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }

        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing retrieve: " + xmlNode.OuterXml);

            string parsedName = parseString(variables, name);
            string stringData = string.Empty;

            if (url != null)
                stringData = RetrieveUrl(variables);
            else
                stringData = ReadFile(variables);

            // Set variable
            if (stringData != null) {
                setVariable(variables, parsedName, stringData);
            }
        }

        // Retrieves an URL
        private string RetrieveUrl(Dictionary<string, string> variables) {
            string parsedUrl = parseString(variables, url);
            string pageContents = string.Empty;

            if (DebugMode) logger.Debug("Retrieving URL: {0}", parsedUrl);

            // Try to grab the document
            try {
                WebGrabber grabber = new WebGrabber(parsedUrl);
                grabber.Request.Accept = "text/xml,application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,image/png,*/*;q=0.5";
                grabber.UserAgent = userAgent;
                grabber.Encoding = encoding;
                grabber.Timeout = timeout;
                grabber.TimeoutIncrement = timeoutIncrement;
                grabber.MaxRetries = maxRetries;
                grabber.AllowUnsafeHeader = allowUnsafeHeader;
                grabber.Debug = DebugMode;

                // Keep session / chaining
                string sessionKey = "urn://scraper/header/" + grabber.Request.RequestUri.Host;
                if (variables.ContainsKey(sessionKey))
                    grabber.CookieHeader = variables[sessionKey];

                // Retrieve the document
                if (grabber.GetResponse()) {
                    // save the current session
                    setVariable(variables, sessionKey, grabber.CookieHeader);
                    // save the contents of the page
                    pageContents = grabber.GetString();
                }
            }
            catch (Exception e) {
                if (e is ThreadAbortException)
                    throw e;

                logger.Warn("Could not connect to " + parsedUrl, e);
            }
            return pageContents;
        }

        // Reads a file
        private string ReadFile(Dictionary<string, string> variables) {
            string parsedFile = parseString(variables, file);
            string fileContents = string.Empty;

            if (System.IO.File.Exists(parsedFile)) {

                if (DebugMode) logger.Debug("Reading file: {0}", parsedFile);

                try {
                    StreamReader streamReader;
                    if (encoding != null) streamReader = new StreamReader(parsedFile, encoding);
                    else streamReader = new StreamReader(parsedFile);

                    fileContents = streamReader.ReadToEnd();
                    streamReader.Close();
                }
                catch (Exception e) {
                    if (e is ThreadAbortException)
                        throw e;

                    logger.Warn("Could not read file: " + parsedFile, e);
                }
            }
            else {
                if (DebugMode) logger.Debug("File does not exist: {0}", parsedFile);
            }

            return fileContents;
        }

        

        #endregion
    }
}
