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
    /// OpmlHead
    /// </summary>
    [Serializable]
    public class OpmlHead
    {
        private string title;
        private string dateCreated;
        private string dateModified;
        private string ownerName;
        private string ownerEmail;
        private string link;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [XmlElement("title")]
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
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        [XmlElement("dateCreated")]
        public string DateCreated
        {
            get 
            { 
                return dateCreated; 
            }

            set
            { 
                dateCreated = value; 
            }
        }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <value>The date modified.</value>
        [XmlElement("dateModified")]
        public string DateModified
        {
            get 
            { 
                return dateModified; 
            }

            set 
            { 
                dateModified = value; 
            }
        }

        /// <summary>
        /// Gets or sets the name of the owner.
        /// </summary>
        /// <value>The name of the owner.</value>
        [XmlElement("ownerName")]
        public string OwnerName
        {
            get 
            { 
                return ownerName; 
            }

            set 
            { 
                ownerName = value; 
            }
        }

        /// <summary>
        /// Gets or sets the owner email.
        /// </summary>
        /// <value>The owner email.</value>
        [XmlElement("ownerEmail")]
        public string OwnerEmail
        {
            get 
            { 
                return ownerEmail; 
            }

            set 
            { 
                ownerEmail = value; 
            }
        }

        /// <summary>
        /// Gets or sets the link.
        /// </summary>
        /// <value>The link.</value>
        [XmlElement("link")]
        public string Link
        {
            get 
            { 
                return link; 
            }

            set 
            { 
                link = value; 
            }
        }
    }
}
