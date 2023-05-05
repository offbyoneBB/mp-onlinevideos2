using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Extensions
{
    public static class CookieExtension
    {
        public static string Serialize(this CookieContainer cookieContainer)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, cookieContainer);
                var bytes = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(bytes, 0, bytes.Length);
                return Convert.ToBase64String(bytes);
            }
        }

        public static CookieContainer Deserialize(this string cookieText)
        {
            try
            {
                var bytes = Convert.FromBase64String(cookieText);
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    return (CookieContainer)new BinaryFormatter().Deserialize(stream);
                }
            }
            catch
            {
                //Ignore if the string is not valid.
            }

            return null;
        }
    }
}
