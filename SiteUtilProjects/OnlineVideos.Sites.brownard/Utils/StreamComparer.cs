using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    class StreamComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            int x_kbps = 0;
            if (!int.TryParse(Regex.Match(x, @"(\d+) kbps").Groups[1].Value, out x_kbps)) return 1;
            int y_kbps = 0;
            if (!int.TryParse(Regex.Match(y, @"(\d+) kbps").Groups[1].Value, out y_kbps)) return -1;
            int compare = x_kbps.CompareTo(y_kbps);
            if (compare != 0)
                return compare;
            return x.CompareTo(y);
        }
    }
}
