using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using MediaPortal.Configuration;
using DirectShowLib;
using DShowNET.Helper;

namespace OnlineVideos.MediaPortal1
{
    #pragma warning disable 1690

    /// <summary>
    /// Description of Configuration.
    /// </summary>
    public partial class Configuration : Form
    {
        ConfigurationPlayer confPlayer;
        OnlineVideosWebservice.Site[] onlineSites = null;

        public Configuration()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            siteColumnLanguage.AspectToStringConverter = delegate(object o)
            {
                return PluginConfiguration.GetLanguageInUserLocale(o as string);
            };

            propertyGridUserConfig.BrowsableAttributes = new AttributeCollection(new CategoryAttribute("OnlineVideosUserConfiguration"));
            propertyGridHoster.BrowsableAttributes = new AttributeCollection(new CategoryAttribute("OnlineVideosUserConfiguration"));
        }

        public void Configuration_Load(object sender, EventArgs e)
        {
            /** fill "Codecs" tab **/
            confPlayer = new ConfigurationPlayer();

            /** fill "General" tab **/
            lblVersion.Text = "Version: " + new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName).Version.ToString();
            tbxScreenName.Text = PluginConfiguration.Instance.BasicHomeScreenName;
            txtThumbLoc.Text = OnlineVideoSettings.Instance.ThumbsDir;
            tbxThumbAge.Text = PluginConfiguration.Instance.ThumbsAge.ToString();
            txtDownloadDir.Text = OnlineVideoSettings.Instance.DownloadDir;
            txtFilters.Text = PluginConfiguration.Instance.FilterArray != null ? string.Join(",", PluginConfiguration.Instance.FilterArray) : "";
            chkUseAgeConfirmation.Checked = OnlineVideoSettings.Instance.UseAgeConfirmation;
            tbxPin.Text = PluginConfiguration.Instance.pinAgeConfirmation;
            tbxWebCacheTimeout.Text = OnlineVideoSettings.Instance.CacheTimeout.ToString();
            tbxUtilTimeout.Text = OnlineVideoSettings.Instance.UtilTimeout.ToString();
            tbxCategoriesTimeout.Text = OnlineVideoSettings.Instance.DynamicCategoryTimeout.ToString();
            tbxWMPBuffer.Text = PluginConfiguration.Instance.wmpbuffer.ToString();
            chkAdaptRefreshRate.Checked = PluginConfiguration.Instance.AllowRefreshRateChange;
            udPlayBuffer.SelectedItem = PluginConfiguration.Instance.playbuffer.ToString();
            chkUseQuickSelect.Checked = PluginConfiguration.Instance.useQuickSelect;
            chkStoreLayoutPerCategory.Checked = PluginConfiguration.Instance.StoreLayoutPerCategory;
            nUPSearchHistoryItemCount.Value = PluginConfiguration.Instance.searchHistoryNum;
            if (PluginConfiguration.Instance.searchHistoryType == PluginConfiguration.SearchHistoryType.Simple)
                rbLastSearch.Checked = true;
            else if (PluginConfiguration.Instance.searchHistoryType == PluginConfiguration.SearchHistoryType.Extended)
                rbExtendedSearchHistory.Checked = true;
            else
                rbOff.Checked = true;

            if (PluginConfiguration.Instance.updateOnStart != null) chkDoAutoUpdate.CheckState = PluginConfiguration.Instance.updateOnStart.Value ? CheckState.Checked : CheckState.Unchecked;
            else chkDoAutoUpdate.CheckState = CheckState.Indeterminate;
            tbxUpdatePeriod.Text = PluginConfiguration.Instance.updatePeriod.ToString();

            chkLatestVideosRandomize.Checked = PluginConfiguration.Instance.LatestVideosRandomize;
            tbxLatestVideosAmount.Text = PluginConfiguration.Instance.LatestVideosMaxItems.ToString();
            tbxLatestVideosOnlineRefresh.Text = PluginConfiguration.Instance.LatestVideosOnlineDataRefresh.ToString();
            tbxLatestVideosGuiRefresh.Text = PluginConfiguration.Instance.LatestVideosGuiDataRefresh.ToString();

            /** fill "Sites" tab **/
            // make sure refelction has run to discover all utils and hosters
            SiteUtilFactory.UtilExists("");
            // load site icons into the ImageList for use with the listviews
            foreach (string imagefile in Directory.GetFiles(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Icons"), "*.png"))
            {
                using (FileStream fs = new FileStream(imagefile, FileMode.Open, FileAccess.Read))
                {
                    imageListSiteIcons.Images.Add(Path.GetFileNameWithoutExtension(imagefile), Image.FromStream(fs));
                }
            }
            bindingSourceSiteSettings.DataSource = OnlineVideoSettings.Instance.SiteSettingsList;
            bindingSourceSitesGroup.DataSource = PluginConfiguration.Instance.SitesGroups;
            siteList.SelectObject(bindingSourceSiteSettings.Current);

            /** fill "Hosters" tab **/
            listBoxHosters.DataSource = Hoster.Base.HosterFactory.GetAllHosters().OrderBy(h => h.getHosterUrl()).ToList();

            /** fill "Groups" Tab **/
            chkAutoGroupByLang.Checked = PluginConfiguration.Instance.autoGroupByLang;
            chkFavFirst.Checked = OnlineVideoSettings.Instance.FavoritesFirst;

            try
            {
                IFilterGraph2 graphBuilder = null;
                IBaseFilter sourceFilter = null;
                OnlineVideos.MPUrlSourceFilter.IFilterState filterState = null;
                OnlineVideos.MPUrlSourceFilter.IFilterStateEx filterStateEx = null;

                try
                {
                    graphBuilder = (IFilterGraph2)new FilterGraph();

                    sourceFilter = FilterFromFile.LoadFilterFromDll("MPUrlSourceSplitter\\MPUrlSourceSplitter.ax", new Guid(OnlineVideos.MPUrlSourceFilter.Downloader.FilterCLSID), true);

                    if (sourceFilter == null)
                    {
                        sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, OnlineVideos.MPUrlSourceFilter.Downloader.FilterName, new Guid(OnlineVideos.MPUrlSourceFilter.Downloader.FilterCLSID));
                    }

                    if (sourceFilter != null)
                    {
                        // check is filter is V2 version

                        filterState = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterState;
                        filterStateEx = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterStateEx;
                    }

                    if (filterStateEx != null)
                    {
                        // filter V2
                        tabProtocols.TabPages.Clear();

                        tabProtocols.TabPages.Add(tabPageHttp);
                        tabProtocols.TabPages.Add(tabPageRtmp);
                        tabProtocols.TabPages.Add(tabPageRtsp);
                        tabProtocols.TabPages.Add(tabPageUdpRtp);

                        System.Net.NetworkInformation.NetworkInterface[] networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

                        comboBoxHttpPreferredNetworkInterface.Items.Add(OnlineVideoSettings.NetworkInterfaceSystemDefault);
                        comboBoxRtmpPreferredNetworkInterface.Items.Add(OnlineVideoSettings.NetworkInterfaceSystemDefault);
                        comboBoxRtspPreferredNetworkInterface.Items.Add(OnlineVideoSettings.NetworkInterfaceSystemDefault);
                        comboBoxUdpRtpPreferredNetworkInterface.Items.Add(OnlineVideoSettings.NetworkInterfaceSystemDefault);

                        foreach (var networkInterface in networkInterfaces)
                        {
                            comboBoxHttpPreferredNetworkInterface.Items.Add(networkInterface.Name);
                            comboBoxRtmpPreferredNetworkInterface.Items.Add(networkInterface.Name);
                            comboBoxRtspPreferredNetworkInterface.Items.Add(networkInterface.Name);
                            comboBoxUdpRtpPreferredNetworkInterface.Items.Add(networkInterface.Name);
                        }

                        comboBoxHttpPreferredNetworkInterface.SelectedIndex = 0;
                        comboBoxRtmpPreferredNetworkInterface.SelectedIndex = 0;
                        comboBoxRtspPreferredNetworkInterface.SelectedIndex = 0;
                        comboBoxUdpRtpPreferredNetworkInterface.SelectedIndex = 0;

                        for (int i = 0; i < comboBoxHttpPreferredNetworkInterface.Items.Count; i++)
                        {
                            String nic = (String)comboBoxHttpPreferredNetworkInterface.Items[i];

                            if (nic == OnlineVideoSettings.Instance.HttpPreferredNetworkInterface)
                            {
                                comboBoxHttpPreferredNetworkInterface.SelectedIndex = i;
                                break;
                            }
                        }

                        for (int i = 0; i < comboBoxRtmpPreferredNetworkInterface.Items.Count; i++)
                        {
                            String nic = (String)comboBoxRtmpPreferredNetworkInterface.Items[i];

                            if (nic == OnlineVideoSettings.Instance.RtmpPreferredNetworkInterface)
                            {
                                comboBoxRtmpPreferredNetworkInterface.SelectedIndex = i;
                                break;
                            }
                        }

                        for (int i = 0; i < comboBoxRtspPreferredNetworkInterface.Items.Count; i++)
                        {
                            String nic = (String)comboBoxRtspPreferredNetworkInterface.Items[i];

                            if (nic == OnlineVideoSettings.Instance.RtspPreferredNetworkInterface)
                            {
                                comboBoxRtspPreferredNetworkInterface.SelectedIndex = i;
                                break;
                            }
                        }

                        for (int i = 0; i < comboBoxUdpRtpPreferredNetworkInterface.Items.Count; i++)
                        {
                            String nic = (String)comboBoxUdpRtpPreferredNetworkInterface.Items[i];

                            if (nic == OnlineVideoSettings.Instance.UdpRtpPreferredNetworkInterface)
                            {
                                comboBoxUdpRtpPreferredNetworkInterface.SelectedIndex = i;
                                break;
                            }
                        }

                        textBoxHttpOpenConnectionTimeout.Text = OnlineVideoSettings.Instance.HttpOpenConnectionTimeout.ToString();
                        textBoxHttpOpenConnectionSleepTime.Text = OnlineVideoSettings.Instance.HttpOpenConnectionSleepTime.ToString();
                        textBoxHttpTotalReopenConnectionTimeout.Text = OnlineVideoSettings.Instance.HttpTotalReopenConnectionTimeout.ToString();

                        textBoxRtmpOpenConnectionTimeout.Text = OnlineVideoSettings.Instance.RtmpOpenConnectionTimeout.ToString();
                        textBoxRtmpOpenConnectionSleepTime.Text = OnlineVideoSettings.Instance.RtmpOpenConnectionSleepTime.ToString();
                        textBoxRtmpTotalReopenConnectionTimeout.Text = OnlineVideoSettings.Instance.RtmpTotalReopenConnectionTimeout.ToString();

                        textBoxRtspOpenConnectionTimeout.Text = OnlineVideoSettings.Instance.RtspOpenConnectionTimeout.ToString();
                        textBoxRtspOpenConnectionSleepTime.Text = OnlineVideoSettings.Instance.RtspOpenConnectionSleepTime.ToString();
                        textBoxRtspTotalReopenConnectionTimeout.Text = OnlineVideoSettings.Instance.RtspTotalReopenConnectionTimeout.ToString();

                        textBoxRtspClientPortMin.Text = OnlineVideoSettings.Instance.RtspClientPortMin.ToString();
                        textBoxRtspClientPortMax.Text = OnlineVideoSettings.Instance.RtspClientPortMax.ToString();

                        textBoxUdpRtpOpenConnectionTimeout.Text = OnlineVideoSettings.Instance.UdpRtpOpenConnectionTimeout.ToString();
                        textBoxUdpRtpOpenConnectionSleepTime.Text = OnlineVideoSettings.Instance.UdpRtpOpenConnectionSleepTime.ToString();
                        textBoxUdpRtpTotalReopenConnectionTimeout.Text = OnlineVideoSettings.Instance.UdpRtpTotalReopenConnectionTimeout.ToString();
                        textBoxUdpRtpReceiveDataCheckInterval.Text = OnlineVideoSettings.Instance.UdpRtpReceiveDataCheckInterval.ToString();
                    }
                    else
                    {
                        // filter V1 or unknown filter
                        tabProtocols.TabPages.Clear();

                        tabProtocols.TabPages.Add(tabPageNotDetectedFilter);
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error Quering Progress: {0}", ex.Message);
                }
                finally
                {
                    if (sourceFilter != null)
                    {
                        DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                        sourceFilter = null;
                    }

                    if (graphBuilder != null)
                    {
                        DirectShowUtil.ReleaseComObject(graphBuilder, 2000);
                        graphBuilder = null;
                    }
                }
            }
            catch
            {
                tabProtocols.TabPages.Clear();

                tabProtocols.TabPages.Add(tabPageNotDetectedFilter);
            }
        }

        void ConfigurationFormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = this.DialogResult;

            if (DialogResult == DialogResult.Cancel)
            {
                dr = MessageBox.Show("Remember to press the same button to exit MediaPortal Configuration.", "Save Changes?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            }

            confPlayer.Stop();

            if (dr == DialogResult.OK || dr == DialogResult.Yes)
            {
                String lsFilter = txtFilters.Text;
                String[] lsFilterArray = lsFilter.Split(new char[] { ',' });
                PluginConfiguration.Instance.FilterArray = lsFilterArray;
                OnlineVideoSettings.Instance.ThumbsDir = txtThumbLoc.Text;
                try { PluginConfiguration.Instance.ThumbsAge = int.Parse(tbxThumbAge.Text); }
                catch { }
                PluginConfiguration.Instance.BasicHomeScreenName = tbxScreenName.Text;
                OnlineVideoSettings.Instance.DownloadDir = txtDownloadDir.Text;
                OnlineVideoSettings.Instance.UseAgeConfirmation = chkUseAgeConfirmation.Checked;
                PluginConfiguration.Instance.pinAgeConfirmation = tbxPin.Text;
                PluginConfiguration.Instance.useQuickSelect = chkUseQuickSelect.Checked;
                PluginConfiguration.Instance.StoreLayoutPerCategory = chkStoreLayoutPerCategory.Checked;
                PluginConfiguration.Instance.searchHistoryNum = (int)nUPSearchHistoryItemCount.Value;
                if (rbLastSearch.Checked)
                    PluginConfiguration.Instance.searchHistoryType = PluginConfiguration.SearchHistoryType.Simple;
                else if (rbExtendedSearchHistory.Checked)
                    PluginConfiguration.Instance.searchHistoryType = PluginConfiguration.SearchHistoryType.Extended;
                else
                    PluginConfiguration.Instance.searchHistoryType = PluginConfiguration.SearchHistoryType.Off;
                int.TryParse(tbxWMPBuffer.Text, out PluginConfiguration.Instance.wmpbuffer);
                int.TryParse(udPlayBuffer.SelectedItem.ToString(), out PluginConfiguration.Instance.playbuffer);
                PluginConfiguration.Instance.AllowRefreshRateChange = chkAdaptRefreshRate.Checked;
                try { OnlineVideoSettings.Instance.CacheTimeout = int.Parse(tbxWebCacheTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.UtilTimeout = int.Parse(tbxUtilTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.DynamicCategoryTimeout = int.Parse(tbxCategoriesTimeout.Text); }
                catch { }
                if (chkDoAutoUpdate.CheckState == CheckState.Indeterminate) PluginConfiguration.Instance.updateOnStart = null;
                else PluginConfiguration.Instance.updateOnStart = chkDoAutoUpdate.Checked;
                PluginConfiguration.Instance.updatePeriod = uint.Parse(tbxUpdatePeriod.Text);
                PluginConfiguration.Instance.autoGroupByLang = chkAutoGroupByLang.Checked;
                OnlineVideoSettings.Instance.FavoritesFirst = chkFavFirst.Checked;
                PluginConfiguration.Instance.LatestVideosRandomize = chkLatestVideosRandomize.Checked;
                try { PluginConfiguration.Instance.LatestVideosMaxItems = uint.Parse(tbxLatestVideosAmount.Text); }
                catch { }
                try { PluginConfiguration.Instance.LatestVideosOnlineDataRefresh = uint.Parse(tbxLatestVideosOnlineRefresh.Text); }
                catch { }
                try { PluginConfiguration.Instance.LatestVideosGuiDataRefresh = uint.Parse(tbxLatestVideosGuiRefresh.Text); }
                catch { }

                OnlineVideoSettings.Instance.HttpPreferredNetworkInterface = (String)comboBoxHttpPreferredNetworkInterface.SelectedItem;
                try { OnlineVideoSettings.Instance.HttpOpenConnectionTimeout = int.Parse(textBoxHttpOpenConnectionTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.HttpOpenConnectionSleepTime = int.Parse(textBoxHttpOpenConnectionSleepTime.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.HttpTotalReopenConnectionTimeout = int.Parse(textBoxHttpTotalReopenConnectionTimeout.Text); }
                catch { }

                OnlineVideoSettings.Instance.RtmpPreferredNetworkInterface = (String)comboBoxRtmpPreferredNetworkInterface.SelectedItem;
                try { OnlineVideoSettings.Instance.RtmpOpenConnectionTimeout = int.Parse(textBoxRtmpOpenConnectionTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.RtmpOpenConnectionSleepTime = int.Parse(textBoxRtmpOpenConnectionSleepTime.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.RtmpTotalReopenConnectionTimeout = int.Parse(textBoxRtmpTotalReopenConnectionTimeout.Text); }
                catch { }

                OnlineVideoSettings.Instance.RtspPreferredNetworkInterface = (String)comboBoxRtspPreferredNetworkInterface.SelectedItem;
                try { OnlineVideoSettings.Instance.RtspOpenConnectionTimeout = int.Parse(textBoxRtspOpenConnectionTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.RtspOpenConnectionSleepTime = int.Parse(textBoxRtspOpenConnectionSleepTime.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.RtspTotalReopenConnectionTimeout = int.Parse(textBoxRtspTotalReopenConnectionTimeout.Text); }
                catch { }

                try { OnlineVideoSettings.Instance.RtspClientPortMin = int.Parse(textBoxRtspClientPortMin.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.RtspClientPortMax = int.Parse(textBoxRtspClientPortMax.Text); }
                catch { }

                OnlineVideoSettings.Instance.UdpRtpPreferredNetworkInterface = (String)comboBoxUdpRtpPreferredNetworkInterface.SelectedItem;
                try { OnlineVideoSettings.Instance.UdpRtpOpenConnectionTimeout = int.Parse(textBoxUdpRtpOpenConnectionTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.UdpRtpOpenConnectionSleepTime = int.Parse(textBoxUdpRtpOpenConnectionSleepTime.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.UdpRtpTotalReopenConnectionTimeout = int.Parse(textBoxUdpRtpTotalReopenConnectionTimeout.Text); }
                catch { }
                try { OnlineVideoSettings.Instance.UdpRtpReceiveDataCheckInterval = int.Parse(textBoxUdpRtpReceiveDataCheckInterval.Text); }
                catch { }

                PluginConfiguration.Instance.Save(false);
            }
        }

        private void btnEditSite_Click(object sender, EventArgs e)
        {
            SiteSettings siteSettings = (SiteSettings)bindingSourceSiteSettings.Current;

            // use a copy of the original settings so anything that is changed can be canceled
            SerializableSettings s = new SerializableSettings() { Sites = new BindingList<SiteSettings>() };
            s.Sites.Add(siteSettings);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            Utils.SiteSettingsToXml(s, ms);
            ms.Position = 0;
            SiteSettings copiedSiteSettings = Utils.SiteSettingsFromXml(new StreamReader(ms))[0];

            CreateEditSite frm = new CreateEditSite();
            frm.Text = "Edit " + siteSettings.Name;
            frm.SiteSettingsBindingSource.DataSource = copiedSiteSettings;
            if (frm.ShowDialog() == DialogResult.OK)
            {
                // make sure the configuration is clean and for the cosen util
                Utils.AddConfigurationValues(frm.SiteUtil, copiedSiteSettings);
                // replace original settings object with the new one
                int index = bindingSourceSiteSettings.IndexOf(siteSettings);
                bindingSourceSiteSettings.RemoveCurrent();
                bindingSourceSiteSettings.Insert(index, copiedSiteSettings);
                bindingSourceSiteSettings.Position = index;
            }
        }

        private void btnAddSite_Click(object sender, EventArgs e)
        {
            CreateEditSite frm = new CreateEditSite();
            frm.Text = "Create new Site";
            SiteSettings site = new SiteSettings();
            site.Name = "New";
            site.UtilName = "GenericSite";
            site.IsEnabled = true;
            frm.SiteSettingsBindingSource.DataSource = site;
            if (frm.ShowDialog() == DialogResult.OK)
            {
                bindingSourceSiteSettings.Position = bindingSourceSiteSettings.Add(site);
            }
        }

        private void btnDeleteSite_Click(object sender, EventArgs e)
        {
            string question = siteList.SelectedObjects.Count > 1 ? string.Format("Delete {0} sites?", siteList.SelectedObjects.Count) : string.Format("Delete {0}?", (siteList.SelectedObject as SiteSettings).Name);
            if (MessageBox.Show(question, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                foreach (SiteSettings site in siteList.SelectedObjects)
                {
                    bindingSourceSiteSettings.Remove(site);
                }
                if (siteList.SelectedObjects.Count > 0)
                {
                    siteList.EnsureModelVisible(siteList.SelectedObjects[0]);
                    siteList.Focus();
                }
            }
        }

        private void btnSiteUp_Click(object sender, EventArgs e)
        {
            siteList.Unsort();

            SiteSettings site = siteList.SelectedObject as SiteSettings;
            siteList.SelectedIndex = -1;
            bindingSourceSiteSettings.SuspendBinding();

            int currentPos = OnlineVideoSettings.Instance.SiteSettingsList.IndexOf(site);
            OnlineVideoSettings.Instance.SiteSettingsList.Remove(site);
            if (currentPos == 0) OnlineVideoSettings.Instance.SiteSettingsList.Add(site);
            else OnlineVideoSettings.Instance.SiteSettingsList.Insert(currentPos - 1, site);

            bindingSourceSiteSettings.ResumeBinding();
            bindingSourceSiteSettings.Position = OnlineVideoSettings.Instance.SiteSettingsList.IndexOf(site);
            bindingSourceSiteSettings.ResetCurrentItem();
        }

        private void btnSiteDown_Click(object sender, EventArgs e)
        {
            siteList.Unsort();

            SiteSettings site = siteList.SelectedObject as SiteSettings;
            siteList.SelectedIndex = -1;
            bindingSourceSiteSettings.SuspendBinding();

            int currentPos = OnlineVideoSettings.Instance.SiteSettingsList.IndexOf(site);
            OnlineVideoSettings.Instance.SiteSettingsList.Remove(site);
            if (currentPos >= OnlineVideoSettings.Instance.SiteSettingsList.Count) OnlineVideoSettings.Instance.SiteSettingsList.Insert(0, site);
            else OnlineVideoSettings.Instance.SiteSettingsList.Insert(currentPos + 1, site);

            bindingSourceSiteSettings.ResumeBinding();
            bindingSourceSiteSettings.Position = OnlineVideoSettings.Instance.SiteSettingsList.IndexOf(site);
            bindingSourceSiteSettings.ResetCurrentItem();
        }

        private void btnImportSite_Click(object sender, EventArgs e)
        {
            try
            {
                ImExportXml dialog = new ImExportXml();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    IList<SiteSettings> sitesFromDlg = Utils.SiteSettingsFromXml(dialog.txtXml.Text);
                    if (sitesFromDlg != null)
                    {
                        foreach (SiteSettings site in sitesFromDlg)
                        {
                            if (OnlineVideoSettings.Instance.SiteSettingsList.Any(ss => ss.Name == site.Name))
                            {
                                MessageBox.Show(string.Format("A site with the name '{0}' is already in the list.", site.Name), "Unique Name required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                OnlineVideoSettings.Instance.SiteSettingsList.Add(site);
                            }
                        }
                        siteList.SelectedObjects = sitesFromDlg as List<SiteSettings>;
                        if (siteList.SelectedObjects.Count > 0)
                        {
                            siteList.EnsureModelVisible(siteList.SelectedObjects[0]);
                            siteList.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Translation.Instance.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPublishSite_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(PluginConfiguration.Instance.email) || string.IsNullOrEmpty(PluginConfiguration.Instance.password))
            {
                if (MessageBox.Show("Do you want to register an Email now?", "Registration required!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RegisterEmail reFrm = new RegisterEmail();
                    reFrm.tbxEmail.Text = PluginConfiguration.Instance.email;
                    reFrm.tbxPassword.Text = PluginConfiguration.Instance.password;
                    if (reFrm.ShowDialog() == DialogResult.OK)
                    {
                        PluginConfiguration.Instance.email = reFrm.tbxEmail.Text;
                        PluginConfiguration.Instance.password = reFrm.tbxPassword.Text;
                    }
                }
                return;
            }
            foreach (SiteSettings site in siteList.SelectedObjects)
            {
                // set current Time to last updated in the xml, so it can be compared later
                DateTime lastUdpBkp = site.LastUpdated;
                site.LastUpdated = DateTime.Now;
                SerializableSettings s = new SerializableSettings() { Sites = new BindingList<SiteSettings>() };
                s.Sites.Add(site);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                Utils.SiteSettingsToXml(s, ms);
                ms.Position = 0;
                System.Xml.XmlDocument siteDoc = new System.Xml.XmlDocument();
                siteDoc.Load(ms);
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Encoding = System.Text.Encoding.UTF8;
                xmlSettings.Indent = true;
                xmlSettings.OmitXmlDeclaration = true;
                StringBuilder sb = new StringBuilder();
                XmlWriter writer = XmlWriter.Create(sb, xmlSettings);
                siteDoc.SelectSingleNode("//Site").WriteTo(writer);
                writer.Flush();
                string siteXmlString = sb.ToString();
                byte[] icon = null;
                string image = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Icons"), site.Name + ".png");
                if (File.Exists(image)) icon = File.ReadAllBytes(image);
                byte[] banner = null;
                image = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Banners"), site.Name + ".png");
                if (File.Exists(image)) banner = File.ReadAllBytes(image);
                bool success = false;
                try
                {
                    string dll = SiteUtilFactory.RequiredDll(site.UtilName);
                    OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                    string msg = "";
                    if (!string.IsNullOrEmpty(dll))
                    {
                        string location = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OnlineVideos\\") + dll + ".dll";
                        if (System.IO.File.Exists(location))
                        {
                            byte[] data = System.IO.File.ReadAllBytes(location);
                            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                            string md5LocalDll = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
                            // check webservice if we need to submit the dll
                            string md5RemoteDll = null;
                            string owner = ws.GetDllOwner(dll, out md5RemoteDll);
                            bool dllFound = md5RemoteDll != null;
                            bool dllsAreEqual = dllFound ? md5RemoteDll == md5LocalDll : false;
                            bool userIsOwner = dllFound ? owner == PluginConfiguration.Instance.email : true;
                            if (!dllsAreEqual)
                            {
                                bool isAdmin = false;
                                if (!userIsOwner)
                                {
                                    if (MessageBox.Show("Only administrators can overwrite a DLL they don't own. I am an Admin. Proceed?", "New DLL - Admin required", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                    {
                                        isAdmin = true;
                                    }
                                }
                                if (userIsOwner || isAdmin)
                                {
                                    string info = dllFound ? "DLL found on server differs from your local file, do you want to update the existing one?" : "Do you want to upload the required dll?";
                                    if (MessageBox.Show(info, "DLL required", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                    {
                                        if (data == null) data = System.IO.File.ReadAllBytes(location);
                                        success = ws.SubmitDll(PluginConfiguration.Instance.email, PluginConfiguration.Instance.password, dll, data, out msg);
                                        MessageBox.Show(msg, success ? Translation.Instance.Success : Translation.Instance.Error, MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                                    }
                                }
                            }
                        }
                    }
                    success = ws.SubmitSite(PluginConfiguration.Instance.email, PluginConfiguration.Instance.password, siteXmlString, icon, banner, dll, out msg);
                    MessageBox.Show(msg, success ? Translation.Instance.Success : Translation.Instance.Error, MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Translation.Instance.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                // if the site was not submitted, restore old last updated date, so saving won't write the wrong value
                if (!success) site.LastUpdated = lastUdpBkp;
            }
        }

        private void btnReportSite_Click(object sender, EventArgs e)
        {
            if (OnlineVideos.Sites.Updater.VersionOnline == null)
            {
                MessageBox.Show("Could not retrieve latest version info from the internet!", Translation.Instance.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!OnlineVideos.Sites.Updater.VersionCompatible)
            {
                MessageBox.Show(string.Format(Translation.Instance.LatestVersionRequired, OnlineVideos.Sites.Updater.VersionOnline), Translation.Instance.Error, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            SiteSettings site = siteList.SelectedObject as SiteSettings;
            SubmitSiteReport ssrFrm = new SubmitSiteReport() { SiteName = site.Name };
            ssrFrm.ShowDialog();
        }

        private void btnImportGlobal_Click(object sender, EventArgs e)
        {
            if (onlineSites == null)
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                onlineSites = ws.GetSitesOverview();
            }

            Dictionary<string, SiteSettings> hashedLocalSites = new Dictionary<string, SiteSettings>();
            foreach (SiteSettings ss in OnlineVideoSettings.Instance.SiteSettingsList) if (!hashedLocalSites.ContainsKey(ss.Name)) hashedLocalSites.Add(ss.Name, ss);

            List<OnlineVideosWebservice.Site> onlyOnlineSites = new List<OnlineVideos.OnlineVideosWebservice.Site>();

            int i = 0;
            while (i < onlineSites.Length)
            {
                if (!hashedLocalSites.ContainsKey(onlineSites[i].Name))
                {
                    int indexOfAt = onlineSites[i].Owner_FK.IndexOf('@');
                    if (indexOfAt > 0) onlineSites[i].Owner_FK = onlineSites[i].Owner_FK.Substring(0, onlineSites[i].Owner_FK.IndexOf('@'));
                    onlyOnlineSites.Add(onlineSites[i]);
                }
                i++;
            }
            ImportGlobalSite frm = new ImportGlobalSite();
            frm.dgvSites.DataSource = onlyOnlineSites;
            if (frm.ShowDialog() == DialogResult.OK)
            {
                List<SiteSettings> importedSites = new List<SiteSettings>();
                Cursor oldCursor = Cursor;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                    foreach (DataGridViewRow dgvr in frm.dgvSites.SelectedRows)
                    {
                        OnlineVideosWebservice.Site onlineSite = (OnlineVideosWebservice.Site)dgvr.DataBoundItem;
                        string siteXml = ws.GetSiteXml(onlineSite.Name);
                        IList<SiteSettings> sitesFromServer = Utils.SiteSettingsFromXml(siteXml);
                        if (sitesFromServer != null)
                        {
                            foreach (SiteSettings site in sitesFromServer)
                            {
                                OnlineVideoSettings.Instance.SiteSettingsList.Add(site);
                                importedSites.Add(site);
                            }
                        }
                        byte[] icon = ws.GetSiteIcon(onlineSite.Name);
                        if (icon != null && icon.Length > 0)
                        {
                            File.WriteAllBytes(Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Icons"), onlineSite.Name + ".png"), icon);
                            if (!imageListSiteIcons.Images.ContainsKey(onlineSite.Name))
                                imageListSiteIcons.Images.Add(onlineSite.Name, Image.FromStream(new MemoryStream(icon)));
                        }
                        icon = ws.GetSiteBanner(onlineSite.Name);
                        if (icon != null && icon.Length > 0)
                        {
                            File.WriteAllBytes(Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Banners"), onlineSite.Name + ".png"), icon);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Translation.Instance.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = oldCursor;
                }
                if (importedSites.Count > 0)
                {
                    siteList.SelectedObjects = importedSites;
                    siteList.EnsureModelVisible(importedSites[0]);
                    siteList.Focus();
                }
            }
        }

        private void btnCreateSite_Click(object sender, EventArgs e)
        {
            string file = Path.Combine(Application.StartupPath, "OnlineVideos.SiteCreator.exe");
            if (System.IO.File.Exists(file))
            {
                MessageBox.Show("Make sure you close Configuration completely before starting to edit sites with the Site Creator.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(file);
            }
            else
            {
                MessageBox.Show("Please reinstall OnlineVideos and select the option 'Site Creation Tool'.", "Tool not installed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void siteList_SelectionChanged(object sender, EventArgs e)
        {
            btnPublishSite.Enabled = siteList.SelectedObjects.Count > 0;
            btnDeleteSite.Enabled = siteList.SelectedObjects.Count > 0;

            if (siteList.SelectedObjects.Count == 0 || siteList.SelectedObjects.Count > 1)
            {
                lblSelectedSite.Text = "";
                iconSite.Image = null;
                propertyGridUserConfig.SelectedObject = null;
                btnEditSite.Enabled = false;
                btnReportSite.Enabled = false;
                btnSiteDown.Enabled = false;
                btnSiteUp.Enabled = false;
            }
            else
            {
                SiteSettings site = siteList.SelectedObjects[0] as SiteSettings;
                string image = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Icons"), site.Name + ".png");
                if (File.Exists(image)) iconSite.ImageLocation = image;
                else iconSite.Image = null;
                propertyGridUserConfig.SelectedObject = SiteUtilFactory.CreateFromShortName(site.UtilName, site);
                lblSelectedSite.Text = site.Name;
                btnEditSite.Enabled = true;
                btnReportSite.Enabled = true;
                btnSiteDown.Enabled = true;
                btnSiteUp.Enabled = true;
            }
        }

        #region General Tab events

        private void chkUseAgeConfirmation_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseAgeConfirmation.Checked)
                tbxPin.Enabled = true;
            else
            {
                tbxPin.Enabled = false;
                MessageBox.Show("This will allow unprotected access to sexually explicit material. Please ensure that anyone given access to MediaPortal has reached the legal age for viewing such content!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void btnBrowseForDlFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDownloadDir.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void bntBrowseFolderForThumbs_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtThumbLoc.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void CheckValidNumber(object sender, CancelEventArgs e)
        {
            string error = null;
            uint value = 0;
            if (!uint.TryParse((sender as TextBox).Text, out value))
            {
                error = (sender as TextBox).Text + " is not a valid number!";
                e.Cancel = true;
            }
            errorProvider1.SetError(sender as TextBox, error);
        }

        private void CheckValidInteger(object sender, CancelEventArgs e)
        {
            string error = null;
            int value = 0;
            if (!int.TryParse((sender as TextBox).Text, out value))
            {
                error = (sender as TextBox).Text + " is not a valid number!";
                e.Cancel = true;
            }
            errorProvider1.SetError(sender as TextBox, error);
        }

        private void searchType_CheckedChanged(object sender, EventArgs e)
        {
            nUPSearchHistoryItemCount.Enabled = rbExtendedSearchHistory.Checked;
        }

        private void btnWiki_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://code.google.com/p/mp-onlinevideos2/wiki/Introduction");
            }
            catch (Exception)
            {
                MessageBox.Show("http://code.google.com/p/mp-onlinevideos2/wiki/Introduction.", "Error opening the Wiki", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Group Tab events
        private void btnAddSitesGroup_Click(object sender, EventArgs e)
        {
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).AddNew();
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).EndCurrentEdit();
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Refresh();

            btnDeleteSitesGroup.Enabled = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Count > 0;
            btnSitesGroupUp.Enabled = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Count > 1;
            btnSitesGroupDown.Enabled = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Count > 1;
        }

        private void btnDeleteSitesGroup_Click(object sender, EventArgs e)
        {
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).RemoveAt(((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Position);

            btnDeleteSitesGroup.Enabled = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Count > 0;
            btnSitesGroupUp.Enabled = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Count > 1;
            btnSitesGroupDown.Enabled = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Count > 1;
        }

        private void btnSitesGroupUp_Click(object sender, EventArgs e)
        {
            int currentPos = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Position;
            SitesGroup item = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Current as SitesGroup;

            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).SuspendBinding();
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).RemoveAt(currentPos);

            if (currentPos == 0) ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).List.Add(item);
            else ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).List.Insert(currentPos - 1, item);

            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).ResumeBinding();
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Refresh();

            bindingSourceSitesGroup.Position = currentPos == 0 ? bindingSourceSitesGroup.Count - 1 : currentPos - 1;
            bindingSourceSitesGroup.ResetCurrentItem();
        }

        private void btnSitesGroupDown_Click(object sender, EventArgs e)
        {
            int currentPos = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Position;
            SitesGroup item = ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Current as SitesGroup;

            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).SuspendBinding();
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).RemoveAt(currentPos);

            if (currentPos >= bindingSourceSitesGroup.Count) ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).List.Insert(0, item);
            else ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).List.Insert(currentPos + 1, item);

            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).ResumeBinding();
            ((CurrencyManager)BindingContext[bindingSourceSitesGroup]).Refresh();

            bindingSourceSitesGroup.Position = currentPos >= bindingSourceSitesGroup.Count - 1 ? 0 : currentPos + 1;
            bindingSourceSitesGroup.ResetCurrentItem();
        }

        private void listBoxSitesNotInGroup_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewSitesNotInGroup.SelectedItems.Count > 0)
            {
                string site = listViewSitesNotInGroup.SelectedItems[0].Text;
                SitesGroup sg = listBoxSitesGroups.SelectedValue as SitesGroup;
                if (sg != null)
                {
                    sg.Sites.Add(site);
                    listViewSitesNotInGroup.Items.RemoveByKey(site);
                    listBoxSitesGroups_SelectedValueChanged(this, EventArgs.Empty);
                }
            }
        }

        private void listBoxSitesInGroup_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewSitesInGroup.SelectedItems.Count > 0)
            {
                string site = listViewSitesInGroup.SelectedItems[0].Text;
                SitesGroup sg = listBoxSitesGroups.SelectedValue as SitesGroup;
                if (sg != null)
                {
                    sg.Sites.Remove(site);
                    listBoxSitesGroups_SelectedValueChanged(this, EventArgs.Empty);
                }
            }
        }

        private void listBoxSitesGroups_SelectedValueChanged(object sender, EventArgs e)
        {
            listViewSitesNotInGroup.Items.Clear();
            listViewSitesInGroup.Items.Clear();
            SitesGroup sg = listBoxSitesGroups.SelectedValue as SitesGroup;
            if (sg != null)
            {
                foreach (var item in OnlineVideoSettings.Instance.SiteSettingsList)
                {
                    if (!sg.Sites.Contains(item.Name)) listViewSitesNotInGroup.Items.Add(item.Name, item.Name, item.Name);
                }
                foreach (string site in sg.Sites)
                {
                    listViewSitesInGroup.Items.Add(site, site, site);
                }
            }
        }

        private void btnBrowseSitesGroupThumb_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // focus the thumbnail textbox so databinding will pick up the new value
                tbxSitesGroupThumb.Focus();
                // set the file path from the dialog
                tbxSitesGroupThumb.Text = openFileDialog1.FileName;
                // focus a different control seo the text is written to the bound object
                tbxSitesGroupDesc.Focus();
            }
        }

        #endregion

        #region Codecs Tab events

        private void btnTestFlv_Click(object sender, EventArgs e)
        {
            confPlayer.Stop();
            string info = "";
            bool succes = confPlayer.Play("http://onlinevideos.nocrosshair.de/TestVideos/Test.flv", videopanel, out info);
            tbxFLVSplitter.Text = info;
            if (succes) chkFLVSplitterInstalled.CheckState = CheckState.Checked; else chkFLVSplitterInstalled.CheckState = CheckState.Unchecked;
        }

        private void btnTestMp4_Click(object sender, EventArgs e)
        {
            confPlayer.Stop();
            string info = "";
            bool succes = confPlayer.Play("http://onlinevideos.nocrosshair.de/TestVideos/Test.mp4", videopanel, out info);
            tbxMP4Splitter.Text = info;
            if (succes) chkMP4SplitterInstalled.CheckState = CheckState.Checked; else chkMP4SplitterInstalled.CheckState = CheckState.Unchecked;
        }

        private void btnTestAvi_Click(object sender, EventArgs e)
        {
            confPlayer.Stop();
            string info = "";
            bool succes = confPlayer.Play("http://onlinevideos.nocrosshair.de/TestVideos/Test.avi", videopanel, out info);
            tbxAVISplitter.Text = info;
            if (succes) chkAVISplitterInstalled.CheckState = CheckState.Checked; else chkAVISplitterInstalled.CheckState = CheckState.Unchecked;
        }

        private void btnTestWmv_Click(object sender, EventArgs e)
        {
            confPlayer.Stop();
            string info = "";
            bool succes = confPlayer.Play("http://onlinevideos.nocrosshair.de/TestVideos/Test.wmv", videopanel, out info);
            tbxWMVSplitter.Text = info;
            if (succes) chkWMVSplitterInstalled.CheckState = CheckState.Checked; else chkWMVSplitterInstalled.CheckState = CheckState.Unchecked;
        }

        private void btnTestMov_Click(object sender, EventArgs e)
        {
            confPlayer.Stop();
            string info = "";
            bool succes = confPlayer.Play("http://onlinevideos.nocrosshair.de/TestVideos/Test.mov", videopanel, out info);
            tbxMOVSplitter.Text = info;
            if (succes) chkMOVSplitterInstalled.CheckState = CheckState.Checked; else chkMOVSplitterInstalled.CheckState = CheckState.Unchecked;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case ConfigurationPlayer.WMGraphNotify:
                    {
                        HandleGraphEvent();
                        break;
                    }
            }
            base.WndProc(ref m);
        }

        private void HandleGraphEvent()
        {
            int hr = 0;
            DirectShowLib.EventCode evCode;
            IntPtr evParam1, evParam2;

            // Make sure that we don't access the media event interface
            // after it has already been released.
            if (confPlayer.mediaEvents == null)
                return;

            // Process all queued events
            while (confPlayer.mediaEvents.GetEvent(out evCode, out evParam1, out evParam2, 0) == 0)
            {
                // Free memory associated with callback, since we're not using it
                hr = confPlayer.mediaEvents.FreeEventParams(evCode, evParam1, evParam2);

                // If this is the end of the clip, reset to beginning
                if (evCode == DirectShowLib.EventCode.Complete)
                {
                    confPlayer.Stop();
                    break;
                }
            }
        }

        #endregion

        #region Hoster Tab events

        private void listBoxHosters_SelectedValueChanged(object sender, EventArgs e)
        {
            Hoster.Base.HosterBase hoster = (sender as ListBox).SelectedItem as Hoster.Base.HosterBase;
            sourceLabel.Text = hoster.GetType().Module.ToString();
            propertyGridHoster.SelectedObject = hoster;
        }

        #endregion

        private void linkLabelFilterDownload_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(linkLabelFilterDownload.Text);
            }
            catch (Exception)
            {
                MessageBox.Show(linkLabelFilterDownload.Text, "Error downloading filter.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    #pragma warning restore 1690

}
