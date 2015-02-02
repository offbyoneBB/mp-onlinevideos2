using System;
using System.Collections.Generic;
using System.Linq;
using OnlineVideos.Hoster.Base;
using System.Text;
using OnlineVideos.AMF;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace OnlineVideos.Hoster
{
    public class Viddler : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "viddler.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {

            Log.Info("!!" + url);
            url = url.TrimEnd('/');
            int ii = url.LastIndexOf('/');
            int p = url.IndexOf('&', ii);
            string videoId = url.Substring(ii + 1, p - ii - 1);

            Dictionary<string, string> options = new Dictionary<string, string>();

            AMFSerializer ser = new AMFSerializer();

            object[] values = new object[4];
            values[0] = videoId;
            values[1] = null;
            values[2] = null;
            values[3] = "false";
            byte[] data = ser.Serialize2("viddlerGateway.getVideoInfo", values);
            AMFObject obj = AMFObject.GetResponse(@"http://www.viddler.com/amfgateway.action", data);

            AMFArray files = obj.GetArray("files");
            for (int i = 0; i < files.Count; i++)
            {
                AMFObject file = files.GetObject(i);
                string nm = String.Format("{0}x{1} {2}K",
                    file.GetDoubleProperty("width"), file.GetDoubleProperty("height"),
                    file.GetDoubleProperty("bitrate"));
                string filename = file.GetStringProperty("filename");
                string path = file.GetStringProperty("path");
                options.Add(nm, decryptPath(path));
            }
            return options;
        }

        private string decryptPath(string path)
        {
            byte[] value = StringToBytes(path);
            BlowfishEngine bf = new BlowfishEngine();
            byte[] key1 = new byte[7] { 107, 108, 117, 99, 122, 121, 107 };
            bf.Init(true, new KeyParameter(key1));
            decrypt(value, bf);
            return Encoding.ASCII.GetString(value);
        }

        private byte[] StringToBytes(string s)
        {
            byte[] value = new byte[s.Length / 2];
            for (int i = 0; i < value.Length; i++)
                value[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return value;
        }

        private void decrypt(byte[] value, BlowfishEngine bf)
        {
            int blockSize = bf.GetBlockSize();
            byte[] vector = new byte[blockSize];
            for (int i = 0; i < blockSize; i++)
                vector[i] = 0;
            for (int i = 0; i < value.Length; i += blockSize)
            {
                byte[] tmp = new byte[blockSize];
                bf.ProcessBlock(vector, 0, tmp, 0);
                int chunk = Math.Min(blockSize, value.Length - i);
                for (int j = 0; j < chunk; j++)
                {
                    vector[j] = value[i + j];
                    value[(i + j)] = (byte)(value[(i + j)] ^ tmp[j]);
                };
            };
        }

        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

    }
}
