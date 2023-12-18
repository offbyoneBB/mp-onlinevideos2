using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using OnlineVideos.WebService;

namespace OnlineVideos.Sites
{
    public static class Updater
    {
        public const string UpdateXmlUrl = "http://raw.githubusercontent.com/offbyoneBB/mp-onlinevideos2/master/MPEI/update.xml";

        public delegate bool ProgressReport(string action, byte? percent);

        static DateTime _lastOnlineVersionCheck = DateTime.MinValue;
        static Version _versionOnline = null;
        static Version _versionLocal = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;

        static DateTime _lastOverviewsRetrieved = DateTime.MinValue;
        static Site[] _onlineSites;
        static Dll[] _onlineDlls;

        static MD5 _md5Service = new MD5CryptoServiceProvider();

        /// <summary>
        /// Breaking API changes for Sites/Skin will change at least the minor version. 
        /// Compatible are only versions with the same major and equal or higher minor number.
        /// </summary>
        public static bool VersionCompatible
        {
            get { return VersionOnline != null && _versionLocal.Major == VersionOnline.Major && _versionLocal.Minor >= VersionOnline.Minor; }
        }

        public static Version VersionLocal
        {
            get { return _versionLocal; }
        }

        public static Version VersionOnline
        {
            get
            {
                try
                {
                    if (DateTime.Now - _lastOnlineVersionCheck > TimeSpan.FromHours(4)) // only check every 4 hours
                    {
                        _lastOnlineVersionCheck = DateTime.Now;
                        XmlDocument xDoc = WebCache.Instance.GetWebData<XmlDocument>(UpdateXmlUrl, cache: false);
                        List<Version> versions = new List<Version>();
                        var versionNode = xDoc.SelectNodes("//PackageClass/GeneralInfo/Version");
                        if (versionNode != null)
                        {
                            foreach (XmlElement versionNodee in versionNode)
                            {
                                versions.Add(new Version(
                                    int.Parse(versionNodee.SelectSingleNode("Major").InnerText),
                                    int.Parse(versionNodee.SelectSingleNode("Minor").InnerText),
                                    int.Parse(versionNodee.SelectSingleNode("Build").InnerText),
                                    int.Parse(versionNodee.SelectSingleNode("Revision").InnerText)));
                            }
                        }
                        if (versions.Any())
                        {
                            versions.Sort();
                            _versionOnline = versions.LastOrDefault();
                        }
                        else
                        {
                            OnlineVideoSettings.Instance.Logger.Warn("No version found in '{0}'", UpdateXmlUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnlineVideoSettings.Instance.Logger.Warn("Error retrieving '{0}' to check for latest version: {1}", UpdateXmlUrl, ex.Message);
                    return null;
                }
                return _versionOnline;
            }
        }

        public static Site[] OnlineSites
        {
            get
            {
                GetRemoteOverviews();
                return _onlineSites;
            }
        }

        public static Dll[] OnlineDlls
        {
            get
            {
                GetRemoteOverviews();
                return _onlineDlls;
            }
        }

        /// <summary>
        /// This method will update the local xml with sites retrieved from the global webservice. It will also download icons, banner and updated dlls.
        /// Make sure the local sites are loaded and all paths (<see cref="OnlineVideoSettings.DllsDir"/>, <see cref="OnlineVideoSettings.ConfigDir"/>, <see cref="OnlineVideoSettings.ThumbsDir"/>) are set before calling.
        /// </summary>
        /// <param name="progressCallback">pointer to the <see cref="ProgressReport"/> delegate that will receive progress information</param>
        /// <param name="onlineSitesToUpdate">update the local sites from this list of online sites, if null (default) the complete global list is retrieved</param>
        /// <param name="onlyUpdateNoAdd">true (default) -> only update already existing local sites, false -> update and add sites from the online list</param>
        /// <param name="skipCategories">do not update the categories of existing local sites if true (default is false so categories will also be updated)</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>true</term><description>new dlls were downloaded during the update</description></item>
        /// <item><term>false</term><description>no changes to the local sites or dlls were done</description></item>
        /// <item><term>null</term><description>update changed only the local xml</description></item>
        /// </list>
        /// </returns>
        public static bool? UpdateSites(ProgressReport progressCallback = null, List<Site> onlineSitesToUpdate = null, bool onlyUpdateNoAdd = true, bool skipCategories = false)
        {
            bool newDllsDownloaded = false;
            bool saveRequired = false;
            try
            {
                if (progressCallback != null) progressCallback.Invoke(Translation.Instance.CheckingForPluginUpdate, 0);
                if (!VersionCompatible) return false;
                if (progressCallback != null) progressCallback.Invoke(Translation.Instance.RetrievingRemoteSites, 2);
                GetRemoteOverviews();
                if (onlineSitesToUpdate == null && _onlineSites != null) onlineSitesToUpdate = _onlineSites.ToList();
                if (onlineSitesToUpdate == null || onlineSitesToUpdate.Count == 0) return false;
                if (progressCallback != null) if (!progressCallback.Invoke(null, 10)) return false;
                Dictionary<string, bool> requiredDlls = new Dictionary<string, bool>();
                for (int i = 0; i < onlineSitesToUpdate.Count; i++)
                {
                    Site onlineSite = onlineSitesToUpdate[i];
                    SiteSettings localSite = null;
                    int localSiteIndex = OnlineVideoSettings.Instance.GetSiteByName(onlineSite.Name, out localSite);
                    if (localSiteIndex == -1)
                    {
                        // add
                        if (!onlyUpdateNoAdd)
                        {
                            // remember what dlls are required and check for changed dlls later
                            if (!string.IsNullOrEmpty(onlineSite.RequiredDll)) requiredDlls[onlineSite.RequiredDll] = true;
                            if (progressCallback != null) progressCallback.Invoke(onlineSite.Name, null);
                            localSite = GetRemoteSite(onlineSite.Name);
                            if (localSite != null)
                            {
                                // disable local site if broken
                                if (onlineSite.State == SiteState.Broken) localSite.IsEnabled = false;
                                OnlineVideoSettings.Instance.AddSite(localSite);
                                saveRequired = true;
                            }
                        }
                    }
                    else // update
                    {
                        // remember what dlls are required and check for changed dlls later (regardless of lastUpdated on site)
                        if (!string.IsNullOrEmpty(onlineSite.RequiredDll)) requiredDlls[onlineSite.RequiredDll] = true;
                        // get site if updated on server
                        if ((onlineSite.LastUpdated - localSite.LastUpdated).TotalMinutes > 2)
                        {
                            // don't show the name of that site while updating if it is an adult site and the pin has not been entered yet
                            bool preventMessageDuetoAdult = (localSite.ConfirmAge && OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed);
                            if (progressCallback != null && !preventMessageDuetoAdult) progressCallback.Invoke(localSite.Name, null);
                            SiteSettings updatedSite = GetRemoteSite(onlineSite.Name);
                            if (updatedSite != null)
                            {
                                // keep Categories if flag was set
                                if (skipCategories) updatedSite.Categories = localSite.Categories;
                                OnlineVideoSettings.Instance.SetSiteAt(localSiteIndex, updatedSite);
                                localSite = updatedSite;
                                saveRequired = true;
                            }
                        }
                        // disable local site if status of online site is broken
                        if (onlineSite.State == SiteState.Broken && localSite.IsEnabled)
                        {
                            localSite.IsEnabled = false;
                            OnlineVideoSettings.Instance.SetSiteAt(localSiteIndex, localSite);
                            saveRequired = true;
                        }
                    }
                    if (progressCallback != null)
                        if (!progressCallback.Invoke(null, (byte)(10 + (70 * (i + 1) / onlineSitesToUpdate.Count))))
                            return false;
                }

                if (progressCallback != null) if (!progressCallback.Invoke(null, null)) return false;

                if (requiredDlls.Count > 0)
                {
                    if (progressCallback != null) progressCallback.Invoke(Translation.Instance.RetrievingRemoteDlls, null);

                    // temp target directory for dlls (if exists, delete and recreate)
                    string dllTempDir = Path.Combine(Path.GetTempPath(), "OnlineVideos\\");
                    if (Directory.Exists(dllTempDir)) Directory.Delete(dllTempDir, true);
                    Directory.CreateDirectory(dllTempDir);
                    int dllsToCopy = 0;
                    for (int i = 0; i < _onlineDlls.Length; i++)
                    {
                        Dll anOnlineDll = _onlineDlls[i];
                        if (progressCallback != null) progressCallback.Invoke(anOnlineDll.Name, null);
                        if (requiredDlls.ContainsKey(anOnlineDll.Name))
                        {
                            // update or download dll if needed
                            string location = Path.Combine(OnlineVideoSettings.Instance.DllsDir, anOnlineDll.Name + ".dll");
                            bool download = true;
                            if (File.Exists(location))
                            {
                                byte[] data = null;
                                data = File.ReadAllBytes(location);
                                string md5LocalDll = BitConverter.ToString(_md5Service.ComputeHash(data)).Replace("-", "").ToLower();
                                if (md5LocalDll == anOnlineDll.MD5)
                                {
                                    download = false;
                                }
                                else
                                {
                                    // MD5 is different - check compile time of local dll vs. LastUpdated of remote dll
                                    if (Helpers.FileUtils.RetrieveLinkerTimestamp(location) > anOnlineDll.LastUpdated)
                                    {
                                        download = false; // local dll is most likely self-compiled - do not download server dll
                                        Log.Info("Local '{0}.dll' was compiled later that online version - skipping download", anOnlineDll.Name);
                                    }
                                }
                            }
                            if (download)
                            {
                                // download dll to temp dir first
                                if (DownloadDll(anOnlineDll.Name, dllTempDir + anOnlineDll.Name + ".dll"))
                                {
                                    newDllsDownloaded = true;
                                    // if download was successfull, try to copy to target dir (if not successfull, mark for UAC prompted copy later)
                                    try { File.Copy(dllTempDir + anOnlineDll.Name + ".dll", location, true); }
                                    catch { dllsToCopy++; }
                                }
                            }
                        }
                        if (progressCallback != null) progressCallback.Invoke(null, (byte)(80 + (15 * (i + 1) / _onlineDlls.Length)));
                    }
                    if (dllsToCopy > 0) CopyDlls(dllTempDir, OnlineVideoSettings.Instance.DllsDir);
                }
                if (saveRequired)
                {
                    if (progressCallback != null) progressCallback.Invoke(Translation.Instance.SavingLocalSiteList, 98);
                    OnlineVideoSettings.Instance.SaveSites();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                if (progressCallback != null) progressCallback.Invoke(Translation.Instance.Done, 100);
            }
            if (newDllsDownloaded) return true;
            else if (saveRequired) return null;
            else return false;
        }

        public static SiteSettings GetRemoteSite(string siteName, OnlineVideosService ws = null)
        {
            try
            {
                if (ws == null) ws = new OnlineVideosService() { Timeout = 30000, EnableDecompression = true };
                string siteXml = ws.GetSiteXml(siteName);
                if (siteXml.Length > 0)
                {
                    IList<SiteSettings> sitesFromWeb = SerializableSettings.Deserialize(siteXml);
                    if (sitesFromWeb != null && sitesFromWeb.Count > 0)
                    {
                        // Download images
                        try
                        {
                            string iconPath = Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + siteName + ".png");
                            byte[] icon = ws.GetSiteIconIfChanged(siteName,
                                File.Exists(iconPath) ?
                                    BitConverter.ToString(_md5Service.ComputeHash(File.ReadAllBytes(iconPath))).Replace("-", "").ToLower()
                                    :
                                    null);
                            if (icon != null && icon.Length > 0) File.WriteAllBytes(iconPath, icon);
                        }
                        catch (Exception ex)
                        {
                            OnlineVideoSettings.Instance.Logger.Warn("Error getting Icon for Site '{0}': {1}", siteName, ex.ToString());
                        }
                        try
                        {
                            string bannerPath = Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Banners\" + siteName + ".png");
                            byte[] banner = ws.GetSiteBannerIfChanged(siteName,
                                File.Exists(bannerPath) ?
                                    BitConverter.ToString(_md5Service.ComputeHash(File.ReadAllBytes(bannerPath))).Replace("-", "").ToLower()
                                    :
                                    null);
                            if (banner != null && banner.Length > 0) File.WriteAllBytes(bannerPath, banner);
                        }
                        catch (Exception ex)
                        {
                            OnlineVideoSettings.Instance.Logger.Warn("Error getting Banner for Site '{0}': {1}", siteName, ex.ToString());
                        }

                        // return the site
                        return sitesFromWeb[0];
                    }
                }
            }
            catch (Exception ex)
            {
                OnlineVideoSettings.Instance.Logger.Warn("Error getting remote Site {0}: {1}", siteName, ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Refreshes the <see cref="OnlineSites"/> and <see cref="OnlineDlls"/> from the webservice every ten minutes.
        /// </summary>
        /// <param name="force">forces the refresh, ignoring the ten minutes refresh interval</param>
        /// <returns>true when new data was retieved</returns>
        public static bool GetRemoteOverviews(bool force = false)
        {
            if (DateTime.Now - _lastOverviewsRetrieved > TimeSpan.FromMinutes(10) || force) // only get overviews every 10 minutes
            {
                bool newData = false;
                OnlineVideosService ws = new OnlineVideosService() { Timeout = 30000, EnableDecompression = true };
                try
                {
                    Log.Info("Updater loading Sites Overview from Webservice");
                    _onlineSites = ws.GetSitesOverview();
                    newData = true;
                }
                catch (Exception ex)
                {
                    Log.Warn("Error on getting sites overview from server: {0}", ex.ToString());
                }
                try
                {
                    _onlineDlls = ws.GetDllsOverview();
                    newData = true;
                }
                catch (Exception ex)
                {
                    Log.Warn("Error on getting dlls overview from server: {0}", ex.ToString());
                }
                _lastOverviewsRetrieved = DateTime.Now;
                return newData;
            }
            return false;
        }

        static bool DownloadDll(string dllName, string localPath)
        {
            try
            {
                Log.Info("Downloading '{0}.dll'", dllName);
                OnlineVideosService ws = new OnlineVideosService() { Timeout = 30000, EnableDecompression = true };
                byte[] onlineDllData = ws.GetDll(dllName);
                if (onlineDllData != null && onlineDllData.Length > 0) File.WriteAllBytes(localPath, onlineDllData);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Error getting remote DLL '{0}.dll': {1}", dllName, ex.ToString());
                return false;
            }
        }

        static void CopyDlls(string sourceDir, string targetDir)
        {
            Log.Info("Copy SiteUtil DLLs from {0} to {1}", sourceDir, targetDir);

            // todo : maybe "mkdir" if target dir does not exist?
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c copy /B /V /Y \"" + sourceDir + "OnlineVideos.Sites.*.dll\" \"" + targetDir + "\"";
            psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.ErrorDialog = false;
            try
            {
                Process p = Process.Start(psi);
                p.WaitForExit(10000);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}
