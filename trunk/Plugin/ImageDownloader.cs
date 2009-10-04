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
        public static bool _imagesDone = false;
        public static bool _stopDownload = false;
        public static List<String> _imageLocationList = new List<String>();

        #region OldCode
        /*
        public static void DownloadImages(object sender, DoWorkEventArgs e)
        {
            _stopDownload = false;
            _imagesDone = false;
            _imageLocationList = new List<String>();
            Object[] loArguments = (Object[])e.Argument;
            GUIFacadeControl facadeView = (GUIFacadeControl)loArguments[0];
            List<String> loImageUrlList = (List<String>)loArguments[1];
            String lsThumbLocation = (String)loArguments[2];


            Log.Info("OnlineVideos thumbnails will be saved in {0}", lsThumbLocation);
            //int liSelectedIndex = Convert.ToInt32(loArguments[2]);
            int liIdx = 0;
            List<String> loImageFileList = new List<String>(loImageUrlList.Count);
            foreach (String lsUrl in loImageUrlList)
            {
                Log.Info("Getting image :" + lsUrl);
                liIdx++;
                if (lsUrl == String.Empty)
                {
                    continue;
                }
                if (_stopDownload)
                {
                    break;
                }
                string lsThumb = MediaPortal.Util.Utils.GetThumb(lsUrl);
                //Log.Info("1)lsThumb = "+lsThumb);
                lsThumb = System.IO.Path.GetFileName(lsThumb);
                //Log.Info("2)lsThumb = "+lsThumb);
                string lsThumbsDir = lsThumbLocation;
                if (System.IO.Directory.Exists(lsThumbsDir) == false)
                {
                    System.IO.Directory.CreateDirectory(lsThumbsDir);
                }
                lsThumb = lsThumbsDir + lsThumb;
                //Log.Info("3)lsThumb = "+lsThumb);
                //Log.Info(lsThumb);
                if (System.IO.File.Exists(lsThumb) == false)
                {
                    //Log.Info("lsThumb doesn't exist");
                    String lsFilename = System.IO.Path.GetFileName(lsThumb);
                    //moLog.Info("Filename will be {0}", lsFilename);
                    MediaPortal.Util.Utils.DownLoadImage(lsUrl, lsThumb);
                    System.Threading.Thread.Sleep(25);
                }
                if (System.IO.File.Exists(lsThumb))
                {
                    //Log.Info("lsThumb exist now");
                    //facadeView[liIdx].IconImageBig = lsThumb;
                    loImageFileList.Add(lsThumb);
                }
                else
                {
                    //Log.Info("lsThumb couldn't be created");
                    loImageFileList.Add("");
                    //facadeView[liIdx].IconImageBig = "";
                }
                //facadeView[liIdx].ThumbnailImage = lsThumb;
                _imageLocationList.Add(lsThumb);
                facadeView[liIdx].ItemId = liIdx;
                //Log.Info("Set item {0} with image {1}", facadeView[liIdx].ItemId,facadeView[liIdx].ThumbnailImage);
                facadeView[liIdx].RetrieveArt = true;
                facadeView[liIdx].RefreshCoverArt();

            }
            
            foreach (String lsFileName in loImageFileList)
            {
                liIdx++;
                facadeView[liIdx].IconImageBig = lsFileName;
                facadeView[liIdx].

            }
            
            _imagesDone = true;
            //facadeView.SelectedListItemIndex = liSelectedIndex;

            //GUIControl.SelectItemControl(4755, facadeView.GetID, liSelectedIndex);

        }
        
        public static String DownloadImage(String fsUrl,String fsThumbLocation){
            string lsThumb = MediaPortal.Util.Utils.GetThumb(fsUrl);
                lsThumb = System.IO.Path.GetFileName(lsThumb);
                string lsThumbsDir = fsThumbLocation;
                if (System.IO.Directory.Exists(lsThumbsDir) ==false)
                {
                    System.IO.Directory.CreateDirectory(lsThumbsDir);
                }
                lsThumb =lsThumbsDir + lsThumb;
                //Log.Info(lsThumb);
                if (System.IO.File.Exists(lsThumb) == false)
                {
                    String lsFilename = System.IO.Path.GetFileName(lsThumb);
                    //moLog.Info("Filename will be {0}", lsFilename);
                    MediaPortal.Util.Utils.DownLoadImage(fsUrl, lsThumb);
                }
                if (System.IO.File.Exists(lsThumb))
                {
                    //facadeView[liIdx].IconImageBig = lsThumb;
                    return lsThumb;
                }
                else
                {
                    return "";
                    //facadeView[liIdx].IconImageBig = "";
                }
        }
         */
        #endregion

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

        public static string downloadPoster(String fsUrl, String fsMovieName, string fsLocation)
        {
            String lsPosterLocation = fsLocation + GetSaveFilename(fsMovieName) + "_p.jpg";
            if (System.IO.File.Exists(lsPosterLocation) == false)
            {
                WebClient client = new WebClient();
                Log.Info("downloading Poster image :" + fsUrl);
                client.DownloadFile(fsUrl, lsPosterLocation);
            }
            return lsPosterLocation;
        }

        public static void getImages(List<String> imageUrlList, String ThumbLocation, GUIFacadeControl facadeView)
        {
            Log.Info("Getting images");
            BackgroundWorker worker = new BackgroundWorker();
            Object[] loParms = new Object[4];
            loParms[0] = facadeView;
            loParms[1] = imageUrlList;
            loParms[2] = ThumbLocation;
            worker.DoWork += new DoWorkEventHandler(DownloadImages2);
            //_imagesDone = false;
            worker.RunWorkerAsync(loParms);
        }

        public static string GetThumbFile(string url)
        {
            string name = MediaPortal.Util.Utils.GetThumb(url);
            name = System.IO.Path.GetFileNameWithoutExtension(name)+ "L.jpg";
            return System.IO.Path.Combine(OnlineVideoSettings.getInstance().msThumbLocation, name);
        }

        public static void DownloadImages2(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.CurrentThread.Name = "OnlineVideosImageDownloader";

            //Log.Info("Using thumb directory:{0}", _imageDirectory);
            Log.Info("Downloading images");
            _imageLocationList.Clear();
            _imagesDone = false;
            _stopDownload = false;
            //List<String> imageList = (List<String>)e.Argument;
            //NameValueCollection imgNameUrlList = (NameValueCollection)e.Argument;
            Object[] loArguments = (Object[])e.Argument;
            GUIFacadeControl facadeView = (GUIFacadeControl)loArguments[0];
            List<String> loImageUrlList = (List<String>)loArguments[1];
            String lsThumbLocation = (String)loArguments[2];


            Log.Info("OnlineVideos thumbnails will be saved in {0}", lsThumbLocation);
            //int liSelectedIndex = Convert.ToInt32(loArguments[2]);
            int liIdx = 0;
            //List<String> loImageFileList = new List<String>(loImageUrlList.Count);
            WebClient client = new WebClient();
                        
            string name;
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
                        name = MediaPortal.Util.Utils.GetThumb(aFinalUrl);
                        name = System.IO.Path.GetFileNameWithoutExtension(name);
                        imageLocation = lsThumbLocation + name + "L.jpg";
                        if (System.IO.File.Exists(imageLocation)) break;                                                
                        try
                        {
                            Log.Info("downloading image :" + aFinalUrl);
                            client.DownloadFile(aFinalUrl, imageLocation);
                            break;
                        }
                        catch (Exception)
                        {
                            Log.Info("image not found : " + aFinalUrl);
                            imageLocation = "";
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
            Log.Info("Setting imagesDone to true");
            _imagesDone = true;
        }

    }
}
