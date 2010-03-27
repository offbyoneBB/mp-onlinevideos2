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
//using System.Collections;
//using System.Text;
using Google.GData.Client;

namespace Google.GData.Extensions.Exif {

    /// <summary>
    /// helper to instantiate all factories defined in here and attach 
    /// them to a base object
    /// </summary>
    public class ExifExtensions
    {
        /// <summary>
        /// adds all ExifExtensions to the passed in baseObject
        /// </summary>
        /// <param name="baseObject"></param>
        public static void AddExtension(AtomBase baseObject) 
        {
            baseObject.AddExtension(new ExifTags());
        }
    }
  
    /// <summary>
    /// short table for constants related to exif xml declarations
    /// </summary>
    public class ExifNameTable 
    {
          /// <summary>static string to specify the exif namespace
          /// </summary>
        public const string NSExif  = "http://schemas.google.com/photos/exif/2007";
        /// <summary>static string to specify the used exif prefix</summary>
        public const string ExifPrefix = "exif";
        /// <summary>
        /// represents the tags container element
        /// </summary>
        public const string ExifTags = "tags";
        /// <summary>
        /// represents the distance element
        /// </summary>
        public const string ExifDistance = "distance";
        /// <summary>
        /// represents the exposure element
        /// </summary>
        public const string ExifExposure = "exposure";
        /// <summary>
        /// represents the flash element
        /// </summary>
        public const string ExifFlash = "flash";
        /// <summary>
        /// represents the focallength element
        /// </summary>
        public const string ExifFocalLength = "focallength";
        /// <summary>
        /// represents the fstop element
        /// </summary>
        public const string ExifFStop = "fstop";
        /// <summary>
        /// represents the unique ID element
        /// </summary>
        public const string ExifImageUniqueID = "imageUniqueID";
        /// <summary>
        /// represents the ISO element
        /// </summary>
        public const string ExifISO = "iso";
        /// <summary>
        /// represents the Make element
        /// </summary>
        public const string ExifMake = "make";
        /// <summary>
        /// represents the Model element
        /// </summary>
        public const string ExifModel = "model";
        /// <summary>
        /// represents the Time element
        /// </summary>
        public const string ExifTime = "time";

     }

    

    /// <summary>
    /// Tags container element for the Exif namespace
    /// </summary>
    public class ExifTags : SimpleContainer
    {
        /// <summary>
        /// base constructor, creates an exif:tags representation
        /// </summary>
        public ExifTags() :
            base(ExifNameTable.ExifTags,
                 ExifNameTable.ExifPrefix,
                 ExifNameTable.NSExif)
        {
            this.ExtensionFactories.Add(new ExifDistance());
            this.ExtensionFactories.Add(new ExifExposure());
            this.ExtensionFactories.Add(new ExifFlash());
            this.ExtensionFactories.Add(new ExifFocalLength());
            this.ExtensionFactories.Add(new ExifFStop());
            this.ExtensionFactories.Add(new ExifImageUniqueID());
            this.ExtensionFactories.Add(new ExifISO());
            this.ExtensionFactories.Add(new ExifMake());
            this.ExtensionFactories.Add(new ExifModel());
            this.ExtensionFactories.Add(new ExifTime());
        }
        /// <summary>
        /// returns the media:credit element
        /// </summary>
        public ExifDistance Distance
        {
            get
            {
                return FindExtension(ExifNameTable.ExifDistance,
                                     ExifNameTable.NSExif) as ExifDistance;
            }
            set
            {
                ReplaceExtension(ExifNameTable.ExifDistance,
                                ExifNameTable.NSExif,
                                value);
            }
        }
    
        /// <summary>
        /// returns the ExifExposure element
        /// </summary>
        public ExifExposure Exposure
        {
            get
            {
                return FindExtension(ExifNameTable.ExifExposure,
                                     ExifNameTable.NSExif) as ExifExposure;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifExposure,
                                ExifNameTable.NSExif,
                                value);
            }
        }
    
        /// <summary>
        /// returns the ExifFlash element
        /// </summary>
        public ExifFlash Flash
        {
            get
            {
                return FindExtension(ExifNameTable.ExifFlash,
                                     ExifNameTable.NSExif) as ExifFlash;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifFlash,
                                ExifNameTable.NSExif,
                                value);
            }
        }
    
        /// <summary>
        /// returns the ExifFocalLength element
        /// </summary>
        public ExifFocalLength FocalLength
        {
            get
            {
                return FindExtension(ExifNameTable.ExifFocalLength,
                                     ExifNameTable.NSExif) as ExifFocalLength;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifFocalLength,
                                ExifNameTable.NSExif,
                                value);
            }
        }
    
        /// <summary>
        /// returns the ExifFStop element
        /// </summary>
        public ExifFStop FStop
        {
            get
            {
                return FindExtension(ExifNameTable.ExifFStop,
                                     ExifNameTable.NSExif) as ExifFStop;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifFStop,
                                ExifNameTable.NSExif,
                                value);
            }
        }
    
        /// <summary>
        /// returns the ExifImageUniqueID element
        /// </summary>
        public ExifImageUniqueID ImageUniqueID
        {
            get
            {
                return FindExtension(ExifNameTable.ExifImageUniqueID,
                                     ExifNameTable.NSExif) as ExifImageUniqueID;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifImageUniqueID,
                                ExifNameTable.NSExif,
                                value);
            }
        }


        /// <summary>
        /// returns the ExifISO element
        /// </summary>
        public ExifISO ISO
        {
            get
            {
                return FindExtension(ExifNameTable.ExifISO,
                                     ExifNameTable.NSExif) as ExifISO;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifISO,
                                ExifNameTable.NSExif,
                                value);
            }
        }

        /// <summary>
        /// returns the ExifMake element
        /// </summary>
        public ExifMake Make
        {
            get
            {
                return FindExtension(ExifNameTable.ExifMake,
                                     ExifNameTable.NSExif) as ExifMake;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifMake,
                                ExifNameTable.NSExif,
                                value);
            }
        }

        /// <summary>
        /// returns the ExifModel element
        /// </summary>
        public ExifModel Model
        {
            get
            {
                return FindExtension(ExifNameTable.ExifModel,
                                     ExifNameTable.NSExif) as ExifModel;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifModel,
                                ExifNameTable.NSExif,
                                value);
            }
        }

        /// <summary>
        /// returns the ExifTime element
        /// </summary>
        public ExifTime Time
        {
            get
            {
                return FindExtension(ExifNameTable.ExifTime,
                                     ExifNameTable.NSExif) as ExifTime;
            }
            set
            {
               ReplaceExtension(ExifNameTable.ExifTime,
                                ExifNameTable.NSExif,
                                value);
            }
        }
    }
    // end of ExifTags

    /// <summary>
    /// ExifDistance schema extension describing an distance
    /// </summary>
    public class ExifDistance : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:distance
        /// </summary>
        public ExifDistance()
        : base(ExifNameTable.ExifDistance, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
    
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifDistance(string initValue)
        : base(ExifNameTable.ExifDistance, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }




    /// <summary>
    /// ExifExposure schema extension describing an exposure
    /// </summary>
    public class ExifExposure : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:exposure
        /// </summary>
        public ExifExposure()
        : base(ExifNameTable.ExifExposure, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}

        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifExposure(string initValue)
        : base(ExifNameTable.ExifExposure, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifFlash schema extension describing an flash
    /// </summary>
    public class ExifFlash : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:flash
        /// </summary>
        public ExifFlash()
        : base(ExifNameTable.ExifFlash, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifFlash(string initValue)
        : base(ExifNameTable.ExifFlash, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifFocalLength schema extension describing an focallength
    /// </summary>
    public class ExifFocalLength : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:focallength
        /// </summary>
        public ExifFocalLength()
        : base(ExifNameTable.ExifFocalLength, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifFocalLength(string initValue)
        : base(ExifNameTable.ExifFocalLength, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifFStop schema extension describing an fstop
    /// </summary>
    public class ExifFStop : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:fstop
        /// </summary>
        public ExifFStop()
        : base(ExifNameTable.ExifFStop, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifFStop(string initValue)
        : base(ExifNameTable.ExifFStop, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifImageUniqueID schema extension describing an imageUniqueID
    /// </summary>
    public class ExifImageUniqueID : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:imageUniqueID
        /// </summary>
        public ExifImageUniqueID()
        : base(ExifNameTable.ExifImageUniqueID, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifImageUniqueID(string initValue)
        : base(ExifNameTable.ExifImageUniqueID, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifISO schema extension describing an iso
    /// </summary>
    public class ExifISO : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:iso
        /// </summary>
        public ExifISO()
        : base(ExifNameTable.ExifISO, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifISO(string initValue)
        : base(ExifNameTable.ExifISO, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifMake schema extension describing an make
    /// </summary>
    public class ExifMake : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:make
        /// </summary>
        public ExifMake()
        : base(ExifNameTable.ExifMake, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifMake(string initValue)
        : base(ExifNameTable.ExifMake, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifModel schema extension describing an model
    /// </summary>
    public class ExifModel : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:model
        /// </summary>
        public ExifModel()
        : base(ExifNameTable.ExifModel, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifModel(string initValue)
        : base(ExifNameTable.ExifModel, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }

    /// <summary>
    /// ExifTime schema extension describing an time
    /// </summary>
    public class ExifTime : SimpleElement
    {
        /// <summary>
        /// basse constructor for exif:time
        /// </summary>
        public ExifTime()
        : base(ExifNameTable.ExifTime, ExifNameTable.ExifPrefix, ExifNameTable.NSExif)
         {}
        /// <summary>
        /// base constructor taking an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExifTime(string initValue)
        : base(ExifNameTable.ExifTime, ExifNameTable.ExifPrefix, ExifNameTable.NSExif, initValue)
        {}
    }
}
