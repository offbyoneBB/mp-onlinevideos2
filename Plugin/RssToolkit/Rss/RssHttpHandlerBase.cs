/*=======================================================================
  Copyright (C) Microsoft Corporation.  All rights reserved.
 
  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
  PARTICULAR PURPOSE.
=======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Xml;
using RssToolkit.Rss;

namespace RssToolkit.Rss
{
    /// <summary>
    ///  base class for RssHttpHandler - Generic handler and strongly typed ones are derived from it
    /// </summary>
    /// <typeparam name="RssRssType">RssDocumentBase</typeparam>
    public abstract class RssHttpHandlerBase<RssRssType> : IHttpHandler where RssRssType:RssDocumentBase, new()
    {
        private RssRssType _rss;
        private HttpContext _context;

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
        bool IHttpHandler.IsReusable
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the RSS.
        /// </summary>
        /// <value>The RSS.</value>
        protected RssRssType Rss
        {
            get 
            { 
                return _rss; 
            }
        }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>The context.</value>
        protected HttpContext Context
        {
            get 
            { 
                return _context; 
            }
        }

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"></see> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (context.Response == null)
            {
                throw new ArgumentNullException("context.Response");
            }

            _context = context;

            // create the Rss
            _rss = new RssRssType();

            // parse the rss name and the user name from the query string
            string userName;
            string rssName;
            DocumentType documentType;
            RssHttpHandlerHelper.ParseChannelQueryString(context.Request, out rssName, out userName, out documentType);

            // populate items (call the derived class)
            PopulateRss(rssName, userName);

            // save XML into response
            XmlDocument doc = new XmlDocument();
            string inputXml = _rss.ToXml(documentType);
            if (documentType == DocumentType.Opml)
            {
                inputXml = inputXml.Replace("#FillValue#", context.Request.Url.ToString());
            }

            doc.LoadXml(inputXml);
            context.Response.ContentType = "text/xml";
            doc.Save(context.Response.OutputStream);
        }

        /// <summary>
        /// Populates the RSS.the only method derived classes are supposed to override
        /// </summary>
        /// <param name="rssName">Name of the RSS.</param>
        /// <param name="userName">Name of the user.</param>
        protected virtual void PopulateRss(string rssName, string userName)
        {
        }
    }
}
