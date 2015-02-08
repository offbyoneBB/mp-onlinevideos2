using System;
using System.IO;
using System.Linq;

namespace OnlineVideos.Helpers
{
    public static class FileUtils
    {
        public static string GetThumbFile(string url)
        {
            // gets a CRC code for the given url and returns a full file path to the image: thums_dir\crc.jpg|gif|png
            string possibleExtension = System.IO.Path.GetExtension(url).ToLower();
            if (possibleExtension != ".gif" & possibleExtension != ".jpg" & possibleExtension != ".png") possibleExtension = ".jpg";
            string name = string.Format("Thumbs{0}L{1}", EncryptionUtils.CalculateCRC32(url), possibleExtension);
            return Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Cache\"), name);
        }

        public static string GetSaveFilename(string input)
        {
            string safe = input;
            foreach (char lDisallowed in System.IO.Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(lDisallowed.ToString(), "");
            }
            foreach (char lDisallowed in System.IO.Path.GetInvalidPathChars())
            {
                safe = safe.Replace(lDisallowed.ToString(), "");
            }
            return safe;
        }

        public static string GetNextFileName(string fullFileName)
        {
            if (string.IsNullOrEmpty(fullFileName)) throw new ArgumentNullException("fullFileName");
            if (!File.Exists(fullFileName)) return fullFileName;

            string baseFileName = Path.GetFileNameWithoutExtension(fullFileName);
            string ext = Path.GetExtension(fullFileName);

            string filePath = Path.GetDirectoryName(fullFileName);
            var numbersUsed = Directory.GetFiles(filePath, baseFileName + "_(*)" + ext)
                    .Select(x => Path.GetFileNameWithoutExtension(x).Substring(baseFileName.Length + 1))
                    .Select(x =>
                    {
                        int result;
                        return Int32.TryParse(x.Trim(new char[] { '(', ')' }), out result) ? result : 0;
                    })
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            var firstGap = numbersUsed
                    .Select((x, i) => new { Index = i, Item = x })
                    .FirstOrDefault(x => x.Index != x.Item);
            int numberToUse = firstGap != null ? firstGap.Item : numbersUsed.Count;
            return Path.Combine(filePath, baseFileName) + "_(" + numberToUse + ")" + ext;
        }

        internal static DateTime RetrieveLinkerTimestamp(string filePath)
        {
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }
    }
}
