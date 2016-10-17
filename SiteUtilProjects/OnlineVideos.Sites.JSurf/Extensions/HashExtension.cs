using System;
using System.Text;

namespace OnlineVideos.Sites.JSurf.Extensions
{
    static class HashExtension
    {
        public static string ToHexString(this byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static string GenId(this string userAgent)
        {
            var hmac = HashLib.HashFactory.HMAC.CreateHMAC(HashLib.HashFactory.Crypto.CreateSHA224());
            ASCIIEncoding encoder = new ASCIIEncoding();
            var key = Guid.NewGuid().ToString();
            Byte[] code = encoder.GetBytes(key);
            hmac.Key = code;
            Byte[] hashMe = encoder.GetBytes(userAgent);
            Byte[] hmBytes = hmac.ComputeBytes(hashMe).GetBytes();
            return ToHexString(hmBytes);
        }
    }
}
