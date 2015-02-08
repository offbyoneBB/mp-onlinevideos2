using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Threading;

namespace OnlineVideos.Downloading
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
        /// Downloads images from the <see cref="SearchResultItem.Thumb"/> in a background thread 
        /// and sets the path of the downloaded image to the <see cref="SearchResultItem.ThumbnailImage"/>.
		/// </summary>
        /// <typeparam name="T">must be a <see cref="SearchResultItem"/></typeparam>
        /// <param name="items">list of <see cref="SearchResultItem"/>s to download images for</param>
        public static void GetImages<T>(IList<T> items) where T : SearchResultItem
        {
            StopDownload = false;
            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)items.Count / 5));
            int groups = (int)Math.Ceiling((double)items.Count / groupSize);
            for (int i = 0; i < groups; i++)
            {
                List<T> group = new List<T>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > items.Count ? items.Count - groupSize * i : groupSize); j++)
                {
                    group.Add(items[j]);
                }

                new Thread(o => DownloadImages<T>((List<T>)o))
                {
                    IsBackground = true,
                    Name = "OVThumbs" + i.ToString()
                }.Start(group);
            }
        }

		/// <summary>
        /// Downloads images from the <see cref="SearchResultItem.Thumb"/> in the current thread
        /// and sets the path of the downloaded image to the <see cref="SearchResultItem.ThumbnailImage"/>.
		/// </summary>
        /// <typeparam name="T">must be a <see cref="SearchResultItem"/></typeparam>
        /// <param name="items">list of <see cref="SearchResultItem"/>s to download images for</param>
        public static void DownloadImages<T>(List<T> items) where T : SearchResultItem
		{
			foreach (T item in items)
			{
				if (StopDownload) break;
                
                if (string.IsNullOrEmpty(item.Thumb)) continue;

				foreach (string url in item.Thumb.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
				{
					string imageLocation = string.Empty;

					Uri temp;
					if (Uri.TryCreate(url, UriKind.Absolute, out temp))
					{
						if (temp.IsFile)
						{
							if (File.Exists(url)) imageLocation = url;
						}
						else
						{
                            string thumbFile = string.IsNullOrEmpty(item.ThumbnailImage) ? Helpers.FileUtils.GetThumbFile(url) : item.ThumbnailImage;
							if (File.Exists(thumbFile)) imageLocation = thumbFile;
                            else if (DownloadAndCheckImage(url, thumbFile, item.ImageForcedAspectRatio)) imageLocation = thumbFile;
						}
					}

                    // stop with the first valid image
					if (imageLocation != string.Empty)
					{
                        item.ThumbnailImage = imageLocation;
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
