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
using RssToolkit.Opml;

namespace RssToolkit.Opml
{
    /// <summary>
    /// OpmlBody
    /// </summary>
    [Serializable()]
    public class OpmlBody
    {
        private List<OpmlOutline> outlines = new List<OpmlOutline>();

        /// <summary>
        /// Gets or sets the outlines.
        /// </summary>
        /// <value>The outlines.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), XmlElement("outline")]
        public List<OpmlOutline> Outlines
        {
            get 
            { 
                return outlines; 
            }

            set 
            { 
                outlines = value; 
            }
        }
    }
}
