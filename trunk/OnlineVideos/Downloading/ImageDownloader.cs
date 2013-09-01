using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.IO;

namespace OnlineVideos
{
    public static class ImageDownloader
    {
		[Serializable]
        public struct ResizeOptions
        {
            public static ResizeOptions Default
            {
                get
                {
                    return new ResizeOptions()
                    {
                        MaxSize = 500,
                        Compositing = CompositingQuality.AssumeLinear,
                        Interpolation = InterpolationMode.High,
                        Smoothing = SmoothingMode.HighQuality
                    };
                }
            }
            public int MaxSize;
            public CompositingQuality Compositing;
            public InterpolationMode Interpolation;
            public SmoothingMode Smoothing;
        }

        public static bool StopDownload { get; set; }

		/// <summary>
		/// Downloads thumbs in background and sets the path of the downloaded files on the items.
		/// </summary>
		/// <typeparam name="T">supported are <see cref="Category"/> and <see cref="VideoInfo"/></typeparam>
		/// <param name="itemsWithThumbs"></param>
        public static void GetImages<T>(IList<T> itemsWithThumbs) where T : ISearchResultItem
        {
            StopDownload = false;
            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count / groupSize);
            for (int i = 0; i < groups; i++)
            {
                List<T> a = new List<T>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    a.Add(itemsWithThumbs[j]);
                }

                new System.Threading.Thread(delegate(object o)
                {
					DownloadImages<T>((List<T>)o);
                })
                {
                    IsBackground = true,
                    Name = "OVThumbs" + i.ToString()
                }.Start(a);
            }
        }

		/// <summary>
		/// Downloads thumbs in the same thread and sets the path of the downloaded files on the items.
		/// </summary>
		/// <typeparam name="T">supported are <see cref="Category"/> and <see cref="VideoInfo"/></typeparam>
		/// <param name="myItems"></param>
        public static void DownloadImages<T>(List<T> myItems) where T : ISearchResultItem
		{
			foreach (T item in myItems)
			{
				if (StopDownload) break;

                string[] urls = null;
                float? forcedAspect = null;

                if (item is Category || item is VideoInfo)
                {
                    string thumb = item is Category ? (item as Category).Thumb : (item as VideoInfo).ImageUrl;
                    if (item is VideoInfo) forcedAspect = (item as VideoInfo).ImageForcedAspectRatio;
                    if (string.IsNullOrEmpty(thumb)) continue;
                    urls = thumb.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (item.Other is OnlineVideos.OnlineVideosWebservice.Site)
                {
                    string siteName = (item.Other as OnlineVideos.OnlineVideosWebservice.Site).Name;
                    string localSiteImage = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + siteName + ".png");
					string onlineSiteImage = "http://onlinevideos.nocrosshair.de/Icons/" + siteName + ".png";
                    urls = new string[] { localSiteImage, onlineSiteImage };
                }
                if (urls == null) continue;
				foreach (string aFinalUrl in urls)
				{
					string imageLocation = string.Empty;

					Uri temp;
					if (Uri.TryCreate(aFinalUrl, UriKind.Absolute, out temp))
					{
						if (temp.IsFile)
						{
							if (System.IO.File.Exists(aFinalUrl)) imageLocation = aFinalUrl;
						}
						else
						{
                            string thumbFile = (item is Category || item is VideoInfo) ? Utils.GetThumbFile(aFinalUrl) : urls[0];
							if (System.IO.File.Exists(thumbFile)) imageLocation = thumbFile;
							else if (DownloadAndCheckImage(aFinalUrl, thumbFile, forcedAspect)) imageLocation = thumbFile;
						}
					}

					if (imageLocation != string.Empty)
					{
                        item.ThumbnailImage = imageLocation;
						if (item is Category)
						{
							(item as Category).NotifyPropertyChanged("ThumbnailImage");
						}
                        else if (item is VideoInfo)
						{	
							(item as VideoInfo).NotifyPropertyChanged("ThumbnailImage");
						}
						break;
					}
				}
			}
		}

        public static bool DownloadAndCheckImage(string url, string file, float? forcedAspectRatio = null)
        {
            try
            {
                if (forcedAspectRatio != null && forcedAspectRatio.Value == 0.0f) forcedAspectRatio = null; // don't use 0.0 but null

                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return false;
                request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
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
                float imageAspectRatio = image.Width / (float)image.Height;
                if (image.Width > OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize || image.Height > OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize
                    || (forcedAspectRatio != null && Math.Abs(forcedAspectRatio.Value - imageAspectRatio) > 0.1))
                {
                    int iWidth = Math.Min(image.Width, OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize);
                    int iHeight = Math.Min(image.Height, OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize);

                    if (forcedAspectRatio != null && Math.Abs(forcedAspectRatio.Value - imageAspectRatio) > 0.1) imageAspectRatio = forcedAspectRatio.Value;

                    if (image.Width > image.Height)
                        iHeight = (int)Math.Floor((((float)iWidth) / imageAspectRatio));
                    else
                        iWidth = (int)Math.Floor((imageAspectRatio * ((float)iHeight)));

                    Bitmap tmp = new Bitmap(iWidth, iHeight, image.PixelFormat);
                    using (Graphics g = Graphics.FromImage(tmp))
                    {
                        g.CompositingQuality = OnlineVideoSettings.Instance.ThumbsResizeOptions.Compositing;
                        g.InterpolationMode = OnlineVideoSettings.Instance.ThumbsResizeOptions.Interpolation;
                        g.SmoothingMode = OnlineVideoSettings.Instance.ThumbsResizeOptions.Smoothing;
                        g.DrawImage(image, new Rectangle(0, 0, iWidth, iHeight));
                        image.Dispose();
                        image = tmp;
                    }
                }
                if (image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid && file.EndsWith(".gif"))
                {
                    image.Save(file, System.Drawing.Imaging.ImageFormat.Gif);
                }
                else if (image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Png.Guid && file.EndsWith(".png"))
                {
                    image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
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
                Log.Info("Invalid Image: '{0}' {1}", url, ex.Message);
                return false;
            }
        }

        public static void DeleteOldThumbs(int maxAge, Func<byte, bool> progressCallback)
        {
            int thumbsDeleted = 0;
            try
            {
                DateTime keepdate = DateTime.Now.AddDays(-maxAge);
				FileInfo[] files = new DirectoryInfo(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Cache\")).GetFiles();
                Log.Info("Checking {0} thumbnails for age.", files.Length);
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo f = files[i];
                    if (f.LastWriteTime <= keepdate)
                    {
                        f.Delete();
                        thumbsDeleted++;
                    }
                    if (!progressCallback((byte)((float)i / files.Length * 100))) break;
                }
            }
            catch (Exception threadException)
            {
                Log.Error(threadException);
            }
            finally
            {
                Log.Info("Deleted {0} thumbnails.", thumbsDeleted);
                progressCallback(Byte.MaxValue);
            }
        }
    }
}
