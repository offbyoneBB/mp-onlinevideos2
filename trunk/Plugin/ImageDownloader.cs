using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using MediaPortal.GUI.Library;
using System.Web;
using System.Net;

namespace OnlineVideos
{
    public static class ImageDownloader
    {
        public static bool _stopDownload = false;
        public static List<String> _imageLocationList = new List<String>();

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
            string name = MediaPortal.Util.Utils.GetThumb(url);
            name = System.IO.Path.GetFileNameWithoutExtension(name) + "L.jpg";
            return System.IO.Path.Combine(OnlineVideoSettings.getInstance().msThumbLocation, name);
        }

        public static string DownloadPoster(string url, string name)
        {
            string file = System.IO.Path.Combine(OnlineVideoSettings.getInstance().msThumbLocation, GetSaveFilename(name) + "_p.jpg");
            if (!System.IO.File.Exists(file))
            {
                Log.Info("downloading Poster image :" + url);
                if (DownloadAndCheckImage(url, file)) return file;
                else return "";
            }
            return file;
        }

        public static void GetImages(List<String> imageUrlList, String ThumbLocation, GUIFacadeControl facadeView)
        {
            Log.Info("Getting images");
            BackgroundWorker worker = new BackgroundWorker();
            Object[] loParms = new Object[4];
            loParms[0] = facadeView;
            loParms[1] = imageUrlList;
            loParms[2] = ThumbLocation;
            worker.DoWork += new DoWorkEventHandler(DownloadImages);
            worker.RunWorkerAsync(loParms);
        }

        static void DownloadImages(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.CurrentThread.Name = "OnlineVideosImageDownloader";
            Object[] loArguments = (Object[])e.Argument;
            GUIFacadeControl facadeView = (GUIFacadeControl)loArguments[0];
            List<String> loImageUrlList = (List<String>)loArguments[1];
            String lsThumbLocation = (String)loArguments[2];
            Log.Info("Downloading images to " + lsThumbLocation);
            _imageLocationList.Clear();
            _stopDownload = false;
            int liIdx = 0;
            string name;
            // todo speedup:
            // 1. Walk all and set the ones that already exists
            // 2. Download missing ones multithreaded (5 at a time)
            foreach (String url in loImageUrlList)
            {                
                liIdx++;
                if (_stopDownload)
                {
                    Log.Info("Received Request to stop Download");
                    break;
                }
                string imageLocation = "";
                if (!string.IsNullOrEmpty(url))
                {
                    string[] urls = url.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);                    
                    foreach (string aFinalUrl in urls)
                    {
                        if (System.IO.Path.IsPathRooted(aFinalUrl))
                        {
                            if (System.IO.File.Exists(aFinalUrl))
                            {
                                imageLocation = aFinalUrl;
                            }
                        }
                        else
                        {
                            // gets a CRC code for the given url and returns a file path: thums_dir\crc.jpg
                            name = MediaPortal.Util.Utils.GetThumb(aFinalUrl);
                            name = System.IO.Path.GetFileNameWithoutExtension(name);
                            imageLocation = lsThumbLocation + name + "L.jpg";
                            if (!System.IO.File.Exists(imageLocation))
                            {
                                Log.Info(string.Format("Downloading Image from {0} to {1}", aFinalUrl, name + "L.jpg"));
                                if (!DownloadAndCheckImage(aFinalUrl, imageLocation))
                                {
                                    Log.Info("Image not found : " + aFinalUrl);
                                    imageLocation = "";
                                }
                            }
                        }
                    }                    
                }
                _imageLocationList.Add(imageLocation);

                if (facadeView.Count <= liIdx)
                {
                    break;
                }
                else
                {
                    if (imageLocation != "")
                    {
                        facadeView[liIdx].RetrieveArt = true;
                        facadeView[liIdx].RefreshCoverArt();
                    }
                }
            }
        }

        static bool DownloadAndCheckImage(string url, string file)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return false;
                request.UserAgent = OnlineVideoSettings.UserAgent;
                request.Accept = "*/*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
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
            catch { return false; }
        }
    }
}
