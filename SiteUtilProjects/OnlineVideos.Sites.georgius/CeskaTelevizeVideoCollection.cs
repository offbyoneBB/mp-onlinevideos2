using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace OnlineVideos.Sites.georgius
{
    public class CeskaTelevizeVideoCollection : KeyedCollection<String, CeskaTelevizeVideo>
    {
        protected override string GetKeyForItem(CeskaTelevizeVideo item)
        {
            return item.Url;
        }
    }
}
