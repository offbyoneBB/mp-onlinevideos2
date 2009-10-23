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

namespace RssToolkit.Rss
{
    /// <summary>
    ///  helper class (for RssHtppHandler) to pack and unpack user name and channel to from/to query string
    /// </summary>
    public sealed class RssHttpHandlerHelper 
    {
        private RssHttpHandlerHelper() 
        {
        }

        /// <summary>
        /// // helper to generate link [to the .ashx] containing channel name and (encoded) userName
        /// </summary>
        /// <param name="handlerPath">The handler path.</param>
        /// <param name="channelName">Name of the channel.</param>
        /// <param name="userName">Name of the user.</param>
        /// <returns>string</returns>
        public static string GenerateChannelLink(string handlerPath, string channelName, string userName) 
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(handlerPath);

            if (string.IsNullOrEmpty(userName)) 
            {
                if (!string.IsNullOrEmpty(channelName))
                {
                    stringBuilder.Append("?c=" + HttpUtility.UrlEncodeUnicode(channelName));
                }
            }
            else 
            {
                if (channelName == null) 
                {
                    channelName = string.Empty;
                }

                userName = "." + userName; // not to confuse the encrypted string with real auth ticket for real user
                DateTime ticketDate = DateTime.Now.AddDays(-100); // already expired

                FormsAuthenticationTicket t = new FormsAuthenticationTicket(
                    2, userName, ticketDate, ticketDate.AddDays(2), false, channelName, "/");

                stringBuilder.Append("?t=" + FormsAuthentication.Encrypt(t));
            }

            return stringBuilder.ToString();
        }

        internal static void ParseChannelQueryString(HttpRequest request, out string channelName, out string userName, out DocumentType outputType)
        {
            string outputTypeQs = request.QueryString["outputtype"];
            if (!string.IsNullOrEmpty(outputTypeQs))
            {
                try
                {
                    outputType = (DocumentType)Enum.Parse(typeof(DocumentType), outputTypeQs, true);
                }
                catch (ArgumentException)
                {
                    outputType = DocumentType.Rss;
                }
            }
            else
            {
                outputType = DocumentType.Rss;
            }

            string ticket = request.QueryString["t"];
            if (string.IsNullOrEmpty(ticket)) 
            {
                userName = string.Empty;
                //// optional unencrypted channel name
                channelName = request.QueryString["c"];
            }
            else
            {
                //// encrypted user name and channel name
                FormsAuthenticationTicket t = FormsAuthentication.Decrypt(ticket);
                userName = t.Name.Substring(1); //// remove extra prepended '.'
                channelName = t.UserData;
            }
        }
    }
}
