using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using MediaPortal.GUI.Library;
using System.Net;
using MediaPortal.Dialogs;
using System.Threading;
using System.Drawing;
using MediaPortal.Util;

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
            string possibleExtension = System.IO.Path.GetExtension(url).ToLower();
            if (possibleExtension != ".gif" & possibleExtension != ".jpg") possibleExtension = ".jpg";
            string name = string.Format("Thumbs{0}L{1}", MediaPortal.Util.Utils.EncryptLine(url), possibleExtension);
            return System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, name);
        }

        public static string DownloadPoster(string url, string name)
        {
            string file = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, GetSaveFilename(name) + "_p.jpg");
            if (!System.IO.File.Exists(file))
            {
                Log.Debug("downloading Poster image :" + url);
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
                                //Log.Debug(string.Format("Downloading Image from {0} to {1}", aFinalUrl, thumbFile));
                                if (DownloadAndCheckImage(aFinalUrl, thumbFile))
                                {
                                    item.ThumbnailImage = thumbFile;
                                    item.IconImage = thumbFile;
                                    item.IconImageBig = thumbFile;
                                    if (item.Item is VideoInfo) (item.Item as VideoInfo).ThumbnailImage = thumbFile;
                                    item.RefreshCoverArt();
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

                // resample if needed
                int maxSize = (int)Thumbs.ThumbLargeResolution;
                if (image.Width > maxSize || image.Height > maxSize)
                {
                    int iWidth = maxSize;
                    int iHeight = maxSize;

                    float fAR = (image.Width) / ((float)image.Height);

                    if (image.Width > image.Height)
                        iHeight = (int)Math.Floor((((float)iWidth) / fAR));
                    else
                        iWidth = (int)Math.Floor((fAR * ((float)iHeight)));
                    
                    Bitmap tmp = new Bitmap(iWidth, iHeight, image.PixelFormat);
                    using (Graphics g = Graphics.FromImage(tmp))
                    {
                        g.CompositingQuality = Thumbs.Compositing;
                        g.InterpolationMode = Thumbs.Interpolation;
                        g.SmoothingMode = Thumbs.Smoothing;
                        g.DrawImage(image, new Rectangle(0, 0, iWidth, iHeight));
                        image.Dispose();
                        image = tmp;
                    }
                }

                if (image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid && file.EndsWith(".gif"))
                {
                    image.Save(file, System.Drawing.Imaging.ImageFormat.Gif);
                }
                else
                {
                    image.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                image.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Log.Info("Invalid Image: {0} {1}", url, ex.Message);
                return false; 
            }
        }

        public static void DeleteOldThumbs()
        {
            GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
            if (dlgPrgrs != null)
            {
                dlgPrgrs.Reset();
                dlgPrgrs.DisplayProgressBar = true;
                dlgPrgrs.ShowWaitCursor = false;
                dlgPrgrs.DisableCancel(false);
                dlgPrgrs.SetHeading(OnlineVideoSettings.Instance.BasicHomeScreenName);
                dlgPrgrs.StartModal(GUIOnlineVideos.WindowId);
                dlgPrgrs.SetLine(1, Translation.DeletingOldThumbs);
                dlgPrgrs.Percentage = 0;
            }
            new Thread(delegate()
            {
                int thumbsDeleted = 0;
                try
                {
                    DateTime keepdate = DateTime.Now.AddDays(-OnlineVideoSettings.Instance.thumbAge);
                    FileInfo[] files = new DirectoryInfo(OnlineVideoSettings.Instance.ThumbsDir).GetFiles();
                    Log.Info("Checking {0} thumbnails for age.", files.Length);
                    for (int i = 0; i < files.Length; i++)
                    {
                        FileInfo f = files[i];
                        if (f.LastWriteTime <= keepdate)
                        {
                            f.Delete();
                            thumbsDeleted++;
                        }
                        dlgPrgrs.Percentage = (int)((float)i / files.Length * 100);
                        if (!dlgPrgrs.ShouldRenderLayer()) break;
                    }
                }
                catch (Exception threadException)
                {
                    Log.Error(threadException);
                }
                finally
                {
                    Log.Info("Deleted {0} thumbnails.", thumbsDeleted);
                    if (dlgPrgrs != null) { dlgPrgrs.Percentage = 100; dlgPrgrs.SetLine(1, Translation.Done); dlgPrgrs.Close(); }
                }
            }) { Name = "OnlineVideosThumbnail", IsBackground = true }.Start();
        }
    }
}
