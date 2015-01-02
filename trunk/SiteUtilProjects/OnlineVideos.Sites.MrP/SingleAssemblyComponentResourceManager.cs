using OnlineVideos.Sites.WebAutomation.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation
{

    /// <summary>
    /// This will allow for the looking up of a localised resource embeded as a resource in the main localised properties file
    /// It means we don't have to deploy the localised dlls through the site publishing
    /// </summary>
    class SingleAssemblyComponentResourceManager : System.ComponentModel.ComponentResourceManager
    {
        private Type _contextTypeInfo;
        private CultureInfo _neutralResourcesCulture;
        private List<string> _prevCultures = new List<string>();

        public SingleAssemblyComponentResourceManager(Type t)
            : base(t)
        {
            _contextTypeInfo = t;
        }
        
        protected override ResourceSet InternalGetResourceSet(CultureInfo culture,
            bool createIfNotExists, bool tryParents)
        {
            ResourceSet rs = (ResourceSet)this.ResourceSets[culture];
            if (rs == null)
            {
                string resourceFileName = null;

                //lazy-load default language (without caring about duplicate assignment in race conditions, no harm done);
                if (this._neutralResourcesCulture == null)
                {
                    this._neutralResourcesCulture =
                        GetNeutralResourcesLanguage(this.MainAssembly);
                }

                // if we're asking for the default language, then ask for the
                // invariant (non-specific) resources.
                if (_neutralResourcesCulture.Equals(culture))
                    culture = CultureInfo.InvariantCulture;
                
                resourceFileName = GetResourceFileName(culture);

                // Only bother to try the lookup based on the resource property if we haven't tried this language before
                if (!_prevCultures.Contains(culture.ToString()) && culture != CultureInfo.InvariantCulture)
                {
                    _prevCultures.Add(culture.ToString());
                    // The T4 template resource generator will link the culture specific resources in to the invariant culture resource
                    // We'll try and load the culture specific resource here

                    var content = base.GetString("Resources." + culture);
                    if (content != null)
                    {
                        using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
                        using (var reader = new ResXResourceReader(stream))
                        {
                            rs = new ResourceSet(reader);
                        }
                    }
                }

                if (rs == null)
                    rs = base.InternalGetResourceSet(culture, createIfNotExists, tryParents);
                 
            }
            return rs;
        }

        //private method in framework, had to be re-specified here.
        private static void AddResourceSet(Hashtable localResourceSets,
            CultureInfo culture, ref ResourceSet rs)
        {
            lock (localResourceSets)
            {
                ResourceSet objA = (ResourceSet)localResourceSets[culture];
                if (objA != null)
                {
                    if (!object.Equals(objA, rs))
                    {
                        rs.Dispose();
                        rs = objA;
                    }
                }
                else
                {
                    localResourceSets.Add(culture, rs);
                }
            }
        }
    }
}
