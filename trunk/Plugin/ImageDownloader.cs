using System;
using System.Collections.Generic;
using System.ComponentModel;
using MediaPortal.GUI.Library;
using System.Net;

namespace OnlineVideos
{
    public static class ImageDownloader
    {
        public static bool StopDownload { get; set; }

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

        public static string GetThumbFile(string url)
        {
            // gets a CRC code for the given url and returns a file path: thums_dir\crc.jpg
            string name = MediaPortal.Util.Utils.GetThumb(url);
            name = System.IO.Path.GetFileNameWithoutExtension(name) + "L.jpg";
            return System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, name);
        }

        public static string DownloadPoster(string url, string name)
        {
            string file = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, GetSaveFilename(name) + "_p.jpg");
            if (!System.IO.File.Exists(file))
            {
                Log.Info("downloading Poster image :" + url);
                if (DownloadAndCheckImage(url, file)) return file;
                else return "";
            }
            return file;
        }

        public static void GetImages(GUIFacadeControl facadeView)
        {            
            List<OnlineVideosGuiListItem> itemsNeedingDownload = new List<OnlineVideosGuiListItem>();
            for(int liIdx = 0; liIdx < facadeView.Count;liIdx++)
            {                
                OnlineVideosGuiListItem item = facadeView[liIdx] as OnlineVideosGuiListItem;                
                if (item != null && !string.IsNullOrEmpty(item.ThumbUrl))
                {
                    bool canBeDownloaded = false;
                    string imageLocation = "";
                    string[] urls = item.ThumbUrl.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string aFinalUrl in urls)
                    {
                        if (System.IO.Path.IsPathRooted(aFinalUrl))
                        {
                            if (System.IO.File.Exists(aFinalUrl))
                            {
                                imageLocation = aFinalUrl;
                                break;
                            }
                        }
                        else
                        {
                            string thumbFile = GetThumbFile(aFinalUrl);
                            if (System.IO.File.Exists(thumbFile))
                            {
                                imageLocation = thumbFile;
                                break;
                            }
                            else
                            {
                                canBeDownloaded = true;
                            }
                        }
                    }
                    if (imageLocation != "")
                    {
                        item.ThumbnailImage = imageLocation;
                        item.IconImage = imageLocation;
                        item.IconImageBig = imageLocation;
                        if (item.Item is VideoInfo) (item.Item as VideoInfo).ThumbnailImage = imageLocation;
                    }
                    else
                    {
                        if (canBeDownloaded) itemsNeedingDownload.Add(item);
                    }
                }
            }
            StopDownload = false;
            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsNeedingDownload.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsNeedingDownload.Count / groupSize);
            for (int i = 0; i < groups; i++)
            {
                new System.Threading.Thread(delegate(object o)
                {
                    List<OnlineVideosGuiListItem> myItems = (List<OnlineVideosGuiListItem>)o;
                    foreach (OnlineVideosGuiListItem item in myItems)
                    {
                        if (StopDownload)
                        {
                            Log.Info("Received request to stop downloading thumbs.");
                            break;
                        }
                        string[] urls = item.ThumbUrl.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string aFinalUrl in urls)
                        {
                            if (!System.IO.Path.IsPathRooted(aFinalUrl)) // only urls
                            {
                                string thumbFile = GetThumbFile(aFinalUrl);
                                Log.Debug(string.Format("Downloading Image from {0} to {1}", aFinalUrl, thumbFile));
                                if (DownloadAndCheckImage(aFinalUrl, thumbFile))
                                {
                                    item.ThumbnailImage = thumbFile;
                                    item.IconImage = thumbFile;
                                    item.IconImageBig = thumbFile;
                                    if (item.Item is VideoInfo) (item.Item as VideoInfo).ThumbnailImage = thumbFile;
                                    break;
                                }                                
                            }
                        }
                    }
                }) 
                { 
                    IsBackground = true, 
                    Name = "OnlineVideosImageDownloader" + i.ToString() 
                }.Start(itemsNeedingDownload.GetRange(groupSize * i, groupSize * (i + 1) > itemsNeedingDownload.Count ? itemsNeedingDownload.Count - groupSize * i : groupSize));
            }            
        }

        static bool DownloadAndCheckImage(string url, string file)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return false;
                request.UserAgent = OnlineVideoSettings.USERAGENT;
                request.Accept = "*/*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                request.Timeout = 5000; // don't wait longer than 5 seconds for an image
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                System.IO.Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();
                System.Drawing.Image image = System.Drawing.Image.FromStream(responseStream, true, true);
                image.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
                image.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("Invalid Image: {0} {1}", url, ex.ToString());
                return false; 
            }
        }
    }
}
