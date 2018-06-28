using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    public abstract class KeyValuePairComparer : IComparer<KeyValuePair<string, string>>, IEqualityComparer<KeyValuePair<string, string>>
    {
        public static KeyValuePairComparer KeyOrdinal { get; private set; }
        public static KeyValuePairComparer KeyOrdinalIgnoreCase { get; private set; }

        static KeyValuePairComparer()
        {
            KeyOrdinal = new KeyComparer(StringComparer.Ordinal);
            KeyOrdinalIgnoreCase = new KeyComparer(StringComparer.OrdinalIgnoreCase);
        }

        public abstract bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y);
        public abstract int GetHashCode(KeyValuePair<string, string> obj);
        public abstract int Compare(KeyValuePair<string, string> x, KeyValuePair<string, string> y);


        public class KeyComparer : KeyValuePairComparer
        {
            private StringComparer _comparer;

            public KeyComparer(StringComparer comparer)
            {
                _comparer = comparer;
            }

            public override bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return _comparer.Equals(x.Key, y.Key);
            }

            public override int GetHashCode(KeyValuePair<string, string> obj)
            {
                return _comparer.GetHashCode(obj.Key);
            }

            public override int Compare(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return _comparer.Compare(x.Key, y.Value);
            }
        }
    }
}
