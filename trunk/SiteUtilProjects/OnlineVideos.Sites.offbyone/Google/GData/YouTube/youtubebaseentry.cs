/* Copyright (c) 2006 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
#define USE_TRACING

using System;
using System.Xml;
using System.IO;
using System.Collections;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Extensions.MediaRss;
using Google.GData.Extensions.Exif;
using Google.GData.Extensions.Location;
using Google.GData.Extensions.AppControl;

namespace Google.GData.YouTube {
    /// <summary>
    /// this class only holds a few helper methods for other entry classes inside the youtube namespace
    /// </summary>
    public abstract class YouTubeBaseEntry : AbstractEntry {
        /// <summary>
        /// instead of having 20 extension elements
        /// we have one string based getter
        /// usage is: entry.getPhotoExtension("albumid") to get the element
        /// </summary>
        /// <param name="extension">the name of the extension to look for</param>
        /// <returns>SimpleElement, or NULL if the extension was not found</returns>
        public SimpleElement getYouTubeExtension(string extension) {
            return FindExtension(extension, YouTubeNameTable.NSYouTube) as SimpleElement;
        }

        /// <summary>
        /// instead of having 20 extension elements
        /// we have one string based getter
        /// usage is: entry.getPhotoExtensionValue("albumid") to get the elements value
        /// </summary>
        /// <param name="extension">the name of the extension to look for</param>
        /// <returns>value as string, or NULL if the extension was not found</returns>
        public string getYouTubeExtensionValue(string extension) {
            SimpleElement e = getYouTubeExtension(extension);
            if (e != null) {
                return e.Value;
            }
            return null;
        }

        /// <summary>
        /// instead of having 20 extension elements
        /// we have one string based setter
        /// usage is: entry.setYouTubeExtension("albumid") to set the element
        /// this will create the extension if it's not there
        /// note, you can ofcourse, just get an existing one and work with that 
        /// object: 
        ///     SimpleElement e = entry.getPhotoExtension("albumid");
        ///     e.Value = "new value";  
        /// 
        /// or 
        ///    entry.setPhotoExtension("albumid", "new Value");
        /// </summary>
        /// <param name="extension">the name of the extension to look for</param>
        /// <param name="newValue">the new value for this extension element</param>
        /// <returns>SimpleElement, either a brand new one, or the one
        /// returned by the service</returns>
        public SimpleElement setYouTubeExtension(string extension, string newValue) {
            if (extension == null) {
                throw new System.ArgumentNullException("extension");
            }

            SimpleElement ele = getYouTubeExtension(extension);
            if (ele == null && newValue != null) {
                ele = CreateExtension(extension, YouTubeNameTable.NSYouTube) as SimpleElement;
                if (ele != null) {
                    this.ExtensionElements.Add(ele);
                }
            }

            if (ele == null) {
                throw new System.ArgumentException("invalid extension element specified");
            }

            if (newValue == null && ele != null) {
                DeleteExtensions(extension, YouTubeNameTable.NSYouTube);
            }

            if (ele != null) {
                ele.Value = newValue;
            }

            return ele;
        }

        /// <summary>
        /// description is used in several subclasses. with the version switch it makes sense to move this here to have the same code while it 
        /// is still supported
        /// </summary>
        /// <returns></returns>
        internal string getDescription() {
            if (this.ProtocolMajor == 1) {
                return getYouTubeExtensionValue(YouTubeNameTable.Description);
            }
            return this.Summary.Text;
        }

        /// <summary>
        /// description is used in several subclasses. with the version switch it makes sense to move this here to have the same code while it 
        /// is still supported
        /// </summary>
        /// <returns></returns>
        internal void setDescription(string value) {
            if (this.ProtocolMajor == 1) {
                setYouTubeExtension(YouTubeNameTable.Description, value);
            } else {
                this.Summary.Text = value;
            }
        }
    }
}



