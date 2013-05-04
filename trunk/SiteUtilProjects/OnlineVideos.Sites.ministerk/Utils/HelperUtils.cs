using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils
{
    class HelperUtils
    {
        public static string GetRandomChars(int amount)
        {
            var random = new Random();
            var sb = new System.Text.StringBuilder(amount);
            for (int i = 0; i < amount; i++) sb.Append(System.Text.Encoding.ASCII.GetString(new byte[] { (byte)random.Next(65, 90) }));
            return sb.ToString();
        }
    }
}
