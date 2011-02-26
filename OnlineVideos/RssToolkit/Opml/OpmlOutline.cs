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
using System.Xml.Serialization;

namespace RssToolkit.Opml
{
    /// <summary>
    /// OpmlOutline
    /// </summary>
    [Serializable]
    public class OpmlOutline
    {
        private string text;
        private string title;
        private string xmlUrl;
        private string htmlUrl;
        private string type;

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [XmlAttribute("text")]
        public string Text
        {
            get 
            {
                return text; 
            }

            set 
            { 
                text = value; 
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [XmlAttribute("title")]
        public string Title
        {
            get 
            { 
                return title; 
            }

            set 
            { 
                title = value; 
            }
        }

        /// <summary>
        /// Gets or sets the XML URL.
        /// </summary>
        /// <value>The XML URL.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings"), XmlAttribute("xmlUrl")]
        public string XmlUrl
        {
            get 
            { 
                return xmlUrl; 
            }

            set 
            { 
                xmlUrl = value; 
            }
        }

        /// <summary>
        /// Gets or sets the HTML URL.
        /// </summary>
        /// <value>The HTML URL.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings"), XmlAttribute("htmlUrl")]
        public string HtmlUrl
        {
            get 
            { 
                return htmlUrl; 
            }

            set 
            { 
                htmlUrl = value; 
            }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [XmlAttribute("type")]
        public string Type
        {
            get 
            { 
                return type; 
            }

            set 
            { 
                type = value; 
            }
        }
    }
}
