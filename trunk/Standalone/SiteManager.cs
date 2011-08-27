using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using OnlineVideos;

namespace Standalone
{
    public class SiteManager
    {
        static Version _LocalVersion = new System.Reflection.AssemblyName(typeof(OnlineVideos.OnlineVideoSettings).Assembly.FullName).Version;
        public static Version LocalVersion { get { return _LocalVersion; } }

        static DateTime LastOnlineversionCheck = DateTime.MinValue;
        static Version _OnlineVersion = null;
        public static Version OnlineVersion
        {
            get
            {
                try
                {
                    if (DateTime.Now - LastOnlineversionCheck > TimeSpan.FromHours(4)) // only check every 4 hours
                    {
                        LastOnlineversionCheck = DateTime.Now;
                        string tempFile = System.IO.Path.GetTempFileName();
                        new System.Net.WebClient().DownloadFile("http://mp-onlinevideos2.googlecode.com/svn/trunk/MPEI/update.xml", tempFile);
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.Load(tempFile);
                        var versionNode = xDoc.SelectNodes("//PackageClass/GeneralInfo/Version");
                        List<Version> versions = new List<Version>();
                        foreach (XmlElement versionNodee in versionNode)
                        {
                            versions.Add(new Version(
                            int.Parse(versionNodee.SelectSingleNode("Major").InnerText),
                                int.Parse(versionNodee.SelectSingleNode("Minor").InnerText),
                                    int.Parse(versionNodee.SelectSingleNode("Build").InnerText),
                                        int.Parse(versionNodee.SelectSingleNode("Revision").InnerText)));
                        }
                        File.Delete(tempFile);
                        versions.Sort();
                        _OnlineVersion = versions.LastOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn("Error retrieving {0} to check for latest version: {1}", "http://mp-onlinevideos2.googlecode.com/svn/trunk/MPEI/update.xml", ex.Message);
                    return null;
                }
                return _OnlineVersion;
            }
        }

        static OnlineVideos.OnlineVideosWebservice.Site[] onlineSites;
        public static OnlineVideos.OnlineVideosWebservice.Site[] GetOnlineSites(OnlineVideos.OnlineVideosWebservice.OnlineVideosService ws = null)
        {
            if (ws != null)
            {
                try { onlineSites = ws.GetSitesOverview(); }
                catch { OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn(OnlineVideos.Translation.RetrievingRemoteSites); }
            }
            return onlineSites;
        }

        static OnlineVideos.OnlineVideosWebservice.Dll[] onlineDlls;
        public static OnlineVideos.OnlineVideosWebservice.Dll[] GetOnlineDlls(OnlineVideos.OnlineVideosWebservice.OnlineVideosService ws = null)
        {
            if (ws != null)
            {
                try { onlineDlls = ws.GetDllsOverview(); }
                catch { OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn(OnlineVideos.Translation.RetrievingRemoteDlls); }
            }
            return onlineDlls;
        }

        public static void AutomaticUpdate()
        {
            if (OnlineVersion == null || OnlineVersion > LocalVersion)
            {
                return; // no online version check retrieved (are we offline?), or newer version available (message?)
            }

            OnlineVideos.OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService() { Timeout = 30000 };
            GetOnlineSites(ws);
            GetOnlineDlls(ws);

            if (onlineSites == null || onlineDlls == null) return;

            bool saveRequired = false;
            Dictionary<string, bool> requiredDlls = new Dictionary<string, bool>();

            for (int i = 0; i < OnlineVideoSettings.Instance.SiteSettingsList.Count; i++)
            {
                SiteSettings localSite = OnlineVideoSettings.Instance.SiteSettingsList[i];
                OnlineVideos.OnlineVideosWebservice.Site remoteSite = Array.Find(onlineSites, delegate(OnlineVideos.OnlineVideosWebservice.Site site) { return site.Name == localSite.Name; });
                if (remoteSite != null)
                {
                    // remember what dlls are required and check for changed dlls later (regardless of lastUpdated on site)
                    if (!string.IsNullOrEmpty(remoteSite.RequiredDll)) requiredDlls[remoteSite.RequiredDll] = true;
                    // get site if updated on server
                    if ((remoteSite.LastUpdated - localSite.LastUpdated).TotalMinutes > 2)
                    {
                        SiteSettings updatedSite = GetRemoteSite(remoteSite.Name, ws);
                        if (updatedSite != null)
                        {
                            OnlineVideoSettings.Instance.SiteSettingsList[i] = updatedSite;
                            localSite = updatedSite;
                            saveRequired = true;
                        }
                    }
                    // disable local site if status of online site is broken
                    if (remoteSite.State == OnlineVideos.OnlineVideosWebservice.SiteState.Broken && localSite.IsEnabled)
                    {
                        localSite.IsEnabled = false;
                        OnlineVideoSettings.Instance.SiteSettingsList[i] = localSite;
                        saveRequired = true;
                    }
                }
            }

            if (saveRequired)
            {
                OnlineVideoSettings.Instance.SaveSites();
            }

            if (requiredDlls.Count > 0)
            {
                for (int i = 0; i < onlineDlls.Length; i++)
                {
                    OnlineVideos.OnlineVideosWebservice.Dll anOnlineDll = onlineDlls[i];
                    if (requiredDlls.ContainsKey(anOnlineDll.Name))
                    {
                        DownloadDll(anOnlineDll, ws);
                    }
                }
            }
        }

        public static bool? DownloadDll(OnlineVideos.OnlineVideosWebservice.Dll anOnlineDll, OnlineVideos.OnlineVideosWebservice.OnlineVideosService ws = null)
        {
            if (anOnlineDll == null) return null;
            string location = OnlineVideoSettings.Instance.DllsDir + anOnlineDll.Name + ".dll";
            bool download = true;
            if (System.IO.File.Exists(location))
            {
                byte[] data = null;
                data = System.IO.File.ReadAllBytes(location);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                string md5LocalDll = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
                if (md5LocalDll == anOnlineDll.MD5) download = false;
            }
            if (download)
            {
                try
                {
                    if (ws == null) ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService() { Timeout = 30000 };
                    byte[] onlineDllData = ws.GetDll(anOnlineDll.Name);
                    if (onlineDllData != null && onlineDllData.Length > 0) System.IO.File.WriteAllBytes(location, onlineDllData);
                    return true;
                }
                catch (Exception ex)
                {
                    OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn("Error getting remote DLL {0}: {1}", anOnlineDll.Name, ex.ToString());
                    return false;
                }
            }
            return null;
        }

        public static SiteSettings GetRemoteSite(string siteName, OnlineVideos.OnlineVideosWebservice.OnlineVideosService ws = null)
        {
            try
            {
                if (ws == null) ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService() { Timeout = 30000 };
                string siteXml = ws.GetSiteXml(siteName);
                if (siteXml.Length > 0)
                {
                    IList<SiteSettings> sitesFromWeb = Utils.SiteSettingsFromXml(siteXml);
                    if (sitesFromWeb != null && sitesFromWeb.Count > 0)
                    {
                        // Download images
                        try
                        {
                            byte[] icon = ws.GetSiteIcon(siteName);
                            if (icon != null && icon.Length > 0) File.WriteAllBytes(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + siteName + ".png"), icon);
                        }
                        catch (Exception ex)
                        {
                            OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn("Error getting Icon for Site {0}: {1}", siteName, ex.ToString());
                        }
                        try
                        {
                            byte[] banner = ws.GetSiteBanner(siteName);
                            if (banner != null && banner.Length > 0) File.WriteAllBytes(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Banners\" + siteName + ".png"), banner);

                        }
                        catch (Exception ex)
                        {
                            OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn("Error getting Banner for Site {0}: {1}", siteName, ex.ToString());
                        }

                        // return the site
                        return sitesFromWeb[0];
                    }
                }
            }
            catch (Exception ex)
            {
                OnlineVideos.OnlineVideoSettings.Instance.Logger.Warn("Error getting remote Site {0}: {1}", siteName, ex.ToString());
            }
            return null;
        }
    }
}
