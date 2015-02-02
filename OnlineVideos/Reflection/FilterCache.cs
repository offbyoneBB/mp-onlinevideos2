using System;
using System.ComponentModel;

namespace OnlineVideos.Reflection
{
    internal class FilterCache
    {
        public Attribute[] Attributes;
        public PropertyDescriptorCollection FilteredProperties;
        public bool IsValid(Attribute[] other)
        {
            if (other == null || Attributes == null) return false;

            if (Attributes.Length != other.Length) return false;

            for (int i = 0; i < other.Length; i++)
            {
                if (!Attributes[i].Match(other[i])) return false;
            }

            return true;
        }
    }
}
