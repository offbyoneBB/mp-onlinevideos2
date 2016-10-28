using System;
using System.Collections.Generic;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites.JSurf.Entities
{
    [Serializable]
    public class ExtendedProperties : TrackingInfo, IVideoDetails
    {
        private Dictionary<string, string> _videoProperties = new SerializableDictionary<string, string>();

        public string Other { get; set; }

        public Dictionary<string, string> VideoProperties { get { return _videoProperties; } set { _videoProperties = value; } }

        public Dictionary<string, string> GetExtendedProperties()
        {
            return _videoProperties;
        }

        public override string ToString()
        {
            return Other ?? base.ToString();
        }
    }
}
