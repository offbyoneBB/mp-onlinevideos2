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

using System;
using System.Xml;
using System.Globalization;
using Google.GData.Client;

namespace Google.GData.Extensions.Location {

    /// <summary>
    /// helper to instantiate all factories defined in here and attach 
    /// them to a base object
    /// </summary>
    public class GeoRssExtensions
    {
        /// <summary>
        /// helper to add all MediaRss extensions to a base object
        /// </summary>
        /// <param name="baseObject"></param>
        public static void AddExtension(AtomBase baseObject) 
        {
            baseObject.AddExtension(new GeoRssWhere());
        }
    }


    /// <summary>
    /// short table for constants related to mediaRss declarations
    /// </summary>
    public class GeoNametable 
    {
        /// <summary>static string to specify the georss namespace
        /// </summary>
        /// <summary>static string to specify the GeoRSS namespace supported</summary>
        public const string NSGeoRss = "http://www.georss.org/georss"; 
        /// <summary>static string to specify the GeoRSS prefix used</summary>
        public const string geoRssPrefix = "georss"; 
        /// <summary>static string to specify the KML namespapce supported</summary>    
        public const string NSGeoKml = "http://www.opengis.net/gml";
        /// <summary>static string to specify the KML prefix used</summary>
        public const string geoKmlPrefix = "gml"; 
        /// <summary>static string to specify the the where element</summary>
        public const string GeoRssWhereElement  = "where";
        /// <summary>static string to specify the the point element</summary>
        public const string GeoKmlPointElement  = "Point";
        /// <summary>static string to specify the the pos element</summary>
        public const string GeoKmlPositionElement    = "pos";
    }

    /// <summary>
    /// GEORSS schema extension describing a location. You are only supposed to deal with that one,
    /// not it's subelements.
    /// </summary>
    public class GeoRssWhere : SimpleContainer
    {
        /// <summary>
        /// default constructor for a GeoRSS where element
        /// </summary>
        public GeoRssWhere() :
            base(GeoNametable.GeoRssWhereElement,
                 GeoNametable.geoRssPrefix,
                 GeoNametable.NSGeoRss)
        {
            this.ExtensionFactories.Add(new GeoKmlPoint());
        }

        /// <summary>
        /// Constructor for a GeoRSS where element with an
        /// initial lat and long
        /// </summary>
        /// <param name="latitude">The latitude of the point</param>
        /// <param name="longitude">The longitude of the point</param>
        public GeoRssWhere(double latitude, double longitude) :
            this()
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        /// <summary>
        ///  accessor for the Latitude part 
        /// </summary>
        public double Latitude
        {
            get 
            {
                GeoKmlPosition position = GetPosition(false);
                if (position == null)
                {
                    return -1;
                }
                return position.Latitude;
                
            }
            set
            {
                GeoKmlPosition position = GetPosition(true);
                position.Latitude = value;
                
            }
        }

        /// <summary>
        /// accessor for the Longitude part
        /// </summary>
        public double Longitude
        {
            get
            {
                GeoKmlPosition position = GetPosition(false);
                if (position == null)
                {
                    return -1;
                }
                return position.Longitude;
            }
            set
            {
                GeoKmlPosition position = GetPosition(true);
                position.Longitude = value;
            }

        }

    

        /// <summary>
        /// finds our position element, if we don't have one
        /// creates a new one depending on the fCreate parameter
        /// </summary>
        /// <param name="create">creates the subelements on true</param> 
        /// <returns>GeoKmlPosition</returns>
        protected GeoKmlPosition GetPosition(bool create) 
        {
            GeoKmlPoint point = FindExtension(GeoNametable.GeoKmlPointElement, 
                                              GeoNametable.NSGeoKml) as GeoKmlPoint;

            GeoKmlPosition position = null;

            if (point == null && create)
            {
                point = new GeoKmlPoint();
                this.ExtensionElements.Add(point);
            }
            if (point != null)
            {
                position = point.FindExtension(GeoNametable.GeoKmlPositionElement,
                                               GeoNametable.NSGeoKml) as GeoKmlPosition;


                if (position == null && create)
                {
                    position = new GeoKmlPosition("0 0");
                    point.ExtensionElements.Add(position);
                }
            }
            return position;
        }
    }

    /// <summary>
    /// KmlPoint. Specifies a particular location, by means of a gml position
    /// element, appears as a child of a georss where element
    /// </summary>
    public class GeoKmlPoint : SimpleContainer
    {
        /// <summary>
        /// default constructor ofr a Kml:point element
        /// </summary>
        public GeoKmlPoint() :
            base(GeoNametable.GeoKmlPointElement,
                 GeoNametable.geoKmlPrefix,
                 GeoNametable.NSGeoKml)
        {
            this.ExtensionFactories.Add(new GeoKmlPosition());
        }
    }

    /// <summary>
    /// KmlPos Specifies a latitude/longitude, seperated by a space
    /// appears as a child of a geokmlpoint element
    /// </summary>
    public class GeoKmlPosition : SimpleElement
    {
        /// <summary>
        /// default constructor, creates a position element
        /// </summary>
        public GeoKmlPosition() :
            base(GeoNametable.GeoKmlPositionElement,
                 GeoNametable.geoKmlPrefix,
                 GeoNametable.NSGeoKml)
        {
        }

        /// <summary>
        /// default constructor, takes an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public GeoKmlPosition(string initValue) :
            base(GeoNametable.GeoKmlPositionElement,
                 GeoNametable.geoKmlPrefix,
                 GeoNametable.NSGeoKml, initValue)
        {
        }

        /// <summary>
        /// accessor for Latitude. Works by dynamically parsing
        /// the string that is stored in Value. Will THROW if
        /// that string is incorrectly formated
        /// </summary>
        public double Latitude
        {
            get 
            {
                string []values = this.Value.Split(new char[] {' '});
                return Convert.ToDouble(values[0], CultureInfo.InvariantCulture);
            }
            set 
            {
                string []values = this.Value.Split(new char[] {' '});
                this.Value = value.ToString(CultureInfo.InvariantCulture) + " " + values[1];
            }
        }

        /// <summary>
        /// accessor for Longitude. Works by dynamically parsing
        /// the string that is stored in Value. Will THROW if
        /// that string is incorrectly formated
        /// </summary>
        public double Longitude
        {
            get 
            {
                string []values = this.Value.Split(new char[] {' '});
                return Convert.ToDouble(values[1], CultureInfo.InvariantCulture);
            }
            set 
            {
                string []values = this.Value.Split(new char[] {' '});
                this.Value = values[0] + " " + value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }




}
