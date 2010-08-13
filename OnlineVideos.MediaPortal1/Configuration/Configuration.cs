using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using MediaPortal.Configuration;

namespace OnlineVideos.MediaPortal1
{
    /// <summary>
    /// Description of Configuration.
    /// </summary>
    public partial class Configuration : Form
    {
        public Configuration()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            propertyGridUserConfig.BrowsableAttributes = new AttributeCollection(new CategoryAttribute("OnlineVideosUserConfiguration"));
        }

        public void Configuration_Load(object sender, EventArgs e)
        {
            /** fill "Codecs" tab **/
            SetInfosFromCodecs();

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
            tbxUtilTimeout.Text = PluginConfiguration.Instance.utilTimeout.ToString();
            tbxWMPBuffer.Text = PluginConfiguration.Instance.wmpbuffer.ToString();
            udPlayBuffer.SelectedItem = PluginConfiguration.Instance.playbuffer.ToString();
            chkUseQuickSelect.Checked = PluginConfiguration.Instance.useQuickSelect;
            chkRememberLastSearch.Checked = PluginConfiguration.Instance.rememberLastSearch;
            if (PluginConfiguration.Instance.updateOnStart != null) chkDoAutoUpdate.CheckState = PluginConfiguration.Instance.updateOnStart.Value ? CheckState.Checked : CheckState.Unchecked;
            else chkDoAutoUpdate.CheckState = CheckState.Indeterminate;

            /** fill "Sites" tab **/
            // utils combobox
            foreach (string site in SiteUtilFactory.GetAllNames()) cbSiteUtil.Items.Add(site);
            // language identifiers combobox
            List<string> cultureNames = new List<string>();
            foreach (System.Globalization.CultureInfo ci in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.NeutralCultures))
            {
                string name = ci.Name.IndexOf('-') >= 0 ? ci.Name.Substring(0, ci.Name.IndexOf('-')) : ci.Name;
                if (!cultureNames.Contains(name)) cultureNames.Add(name);
            }
            cultureNames.Add("--");
            cultureNames.Sort();
            cbLanguages.Items.AddRange(cultureNames.ToArray());

            // set bindings            
            bindingSourceSiteSettings.DataSource = OnlineVideoSettings.Instance.SiteSettingsList;
        }

        void SiteListSelectedValueChanged(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            BindingList<RssLink> rssLinks = new BindingList<RssLink>();

            tvGroups_AfterSelect(tvGroups, new TreeViewEventArgs(null, TreeViewAction.Unknown));
            tvGroups.Nodes.Clear();

            iconSite.Image = null;

            if (site != null)
            {
                foreach (Category aCat in site.Categories)
                {
                    if (aCat is RssLink) rssLinks.Add(aCat as RssLink);
                    else if (aCat is Group)
                    {
                        TreeNode aGroupNode = new TreeNode(aCat.Name);
                        aGroupNode.Tag = aCat;
                        foreach (Channel aChannel in (aCat as Group).Channels)
                        {
                            TreeNode node = new TreeNode(aChannel.StreamName);
                            node.Tag = aChannel;
                            aGroupNode.Nodes.Add(node);
                        }
                        tvGroups.Nodes.Add(aGroupNode);
                    }
                }
                tvGroups.ExpandAll();

                Sites.SiteUtilBase siteUtil = SiteUtilFactory.CreateFromShortName(site.UtilName, site);
                propertyGridUserConfig.SelectedObject = siteUtil;

                string image = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + site.Name + ".png";
                if (System.IO.File.Exists(image)) iconSite.ImageLocation = image;
            }

            bindingSourceRssLink.DataSource = rssLinks;
            RssLinkListSelectedIndexChanged(this, EventArgs.Empty);

            btnAddRss.Enabled = site != null;
            btnAddGroup.Enabled = site != null;

            btnDeleteSite.Enabled = site != null;
            btnReportSite.Enabled = site != null;
            btnPublishSite.Enabled = site != null;
            btnSiteUp.Enabled = site != null;
            btnSiteDown.Enabled = site != null;
        }

        void RssLinkListSelectedIndexChanged(object sender, EventArgs e)
        {
            // enable/disable all TextBoxes and Buttons for RssLink if one/none is selected
            txtRssUrl.Enabled = RssLinkList.SelectedIndex > -1;
            txtRssName.Enabled = RssLinkList.SelectedIndex > -1;
            txtRssThumb.Enabled = RssLinkList.SelectedIndex > -1;
            btnDeleteRss.Enabled = RssLinkList.SelectedIndex > -1;
        }

        void BtnAddRssClick(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            RssLink link = new RssLink() { Name = "new", Url = "http://" };
            site.Categories.Add(link);
            ((CurrencyManager)BindingContext[bindingSourceRssLink]).List.Add(link);
            RssLinkList.SelectedIndex = RssLinkList.Items.Count - 1;
            RssLinkListSelectedIndexChanged(this, EventArgs.Empty);
            txtRssName.Focus();
        }

        void BtnDeleteRssClick(object sender, EventArgs e)
        {
            if (RssLinkList.SelectedIndex > -1)
            {
                SiteSettings site = siteList.SelectedItem as SiteSettings;
                RssLink link = RssLinkList.SelectedItem as RssLink;
                ((CurrencyManager)BindingContext[bindingSourceRssLink]).RemoveAt(RssLinkList.SelectedIndex);
                site.Categories.Remove(link);
            }
        }

        void ConfigurationFormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = this.DialogResult;

            if (DialogResult == DialogResult.Cancel)
            {
                dr = MessageBox.Show("If you want to save your changes press Yes.", "Save Changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            }

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
                PluginConfiguration.Instance.rememberLastSearch = chkRememberLastSearch.Checked;
                int.TryParse(tbxWMPBuffer.Text, out PluginConfiguration.Instance.wmpbuffer);
                int.TryParse(udPlayBuffer.SelectedItem.ToString(), out PluginConfiguration.Instance.playbuffer);
                try { OnlineVideoSettings.Instance.CacheTimeout = int.Parse(tbxWebCacheTimeout.Text); }
                catch { }
                try { PluginConfiguration.Instance.utilTimeout = int.Parse(tbxUtilTimeout.Text); }
                catch { }
                if (chkDoAutoUpdate.CheckState == CheckState.Indeterminate) PluginConfiguration.Instance.updateOnStart = null;
                else PluginConfiguration.Instance.updateOnStart = chkDoAutoUpdate.Checked;
                PluginConfiguration.Instance.Save();
            }
        }

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

        private void tvGroups_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                btnSaveChannel.Enabled = true;
                btnDeleteChannel.Enabled = true;
                if (e.Node.Parent == null)
                {
                    tbxChannelName.Enabled = true;
                    tbxChannelName.Text = e.Node.Text;
                    tbxChannelThumb.Enabled = true;
                    tbxChannelThumb.Text = ((Group)e.Node.Tag).Thumb;
                    tbxStreamName.Text = "";
                    tbxStreamName.Enabled = false;
                    tbxStreamUrl.Text = "";
                    tbxStreamUrl.Enabled = false;
                    tbxStreamThumb.Text = "";
                    tbxStreamThumb.Enabled = false;
                    btnAddChannel.Enabled = true;
                }
                else
                {
                    tbxChannelName.Text = "";
                    tbxChannelName.Enabled = false;
                    tbxChannelThumb.Text = "";
                    tbxChannelThumb.Enabled = false;
                    tbxStreamName.Text = e.Node.Text;
                    tbxStreamName.Enabled = true;
                    tbxStreamUrl.Text = ((Channel)e.Node.Tag).Url;
                    tbxStreamUrl.Enabled = true;
                    tbxStreamThumb.Text = ((Channel)e.Node.Tag).Thumb;
                    tbxStreamThumb.Enabled = true;
                    btnAddChannel.Enabled = false;
                }
            }
            else
            {
                tbxChannelName.Text = "";
                tbxChannelName.Enabled = false;
                tbxChannelThumb.Text = "";
                tbxChannelThumb.Enabled = false;
                tbxStreamName.Text = "";
                tbxStreamName.Enabled = false;
                tbxStreamUrl.Text = "";
                tbxStreamUrl.Enabled = false;
                tbxStreamThumb.Text = "";
                tbxStreamThumb.Enabled = false;
                btnSaveChannel.Enabled = false;
                btnDeleteChannel.Enabled = false;
                btnAddChannel.Enabled = false;
            }
        }

        private void btnSaveChannel_Click(object sender, EventArgs e)
        {
            if (tvGroups.SelectedNode != null)
            {
                if (tvGroups.SelectedNode.Parent == null)
                {
                    SiteSettings site = siteList.SelectedItem as SiteSettings;
                    Group group = tvGroups.SelectedNode.Tag as Group;
                    site.Categories.Remove(group);
                    group.Name = tbxChannelName.Text;
                    group.Thumb = tbxChannelThumb.Text != "" ? tbxChannelThumb.Text : null;
                    tvGroups.SelectedNode.Text = tbxChannelName.Text;
                    site.Categories.Add(group);
                }
                else
                {
                    Channel channel = tvGroups.SelectedNode.Tag as Channel;
                    channel.StreamName = tbxStreamName.Text;
                    tvGroups.SelectedNode.Text = tbxStreamName.Text;
                    channel.Url = tbxStreamUrl.Text;
                    channel.Thumb = tbxStreamThumb.Text != "" ? tbxStreamThumb.Text : null;
                }
            }
        }

        private void btnDeleteChannel_Click(object sender, EventArgs e)
        {
            if (tvGroups.SelectedNode != null)
            {
                if (tvGroups.SelectedNode.Parent == null)
                {
                    Group group = tvGroups.SelectedNode.Tag as Group;
                    SiteSettings site = siteList.SelectedItem as SiteSettings;
                    site.Categories.Remove(group);
                    tvGroups.Nodes.Remove(tvGroups.SelectedNode);
                }
                else
                {
                    Channel channel = tvGroups.SelectedNode.Tag as Channel;
                    Group group = tvGroups.SelectedNode.Parent.Tag as Group;
                    group.Channels.Remove(channel);
                    tvGroups.Nodes.Remove(tvGroups.SelectedNode);
                }
                tvGroups.SelectedNode = null;
                tvGroups_AfterSelect(tvGroups, new TreeViewEventArgs(null));
            }
        }

        private void btnAddGroup_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            Group g = new Group();
            g.Name = "New";
            site.Categories.Add(g);
            TreeNode node = new TreeNode("New");
            node.Tag = g;
            tvGroups.Nodes.Add(node);
            tvGroups.SelectedNode = node;
            tbxChannelName.Focus();
        }

        private void btnAddChannel_Click(object sender, EventArgs e)
        {
            Group group = tvGroups.SelectedNode.Tag as Group;
            Channel c = new Channel();
            c.StreamName = "New";
            if (group.Channels == null) group.Channels = new BindingList<Channel>();
            group.Channels.Add(c);
            TreeNode node = new TreeNode("New");
            node.Tag = c;
            tvGroups.SelectedNode.Nodes.Add(node);
            tvGroups.SelectedNode = node;
            tbxStreamName.Focus();
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


        void SetInfosFromCodecs()
        {
            CodecConfiguration cc = CodecConfiguration.Instance;

            chkFLVSplitterInstalled.Checked = cc.FLV_Splitter.IsInstalled;
            tbxFLVSplitter.Text = cc.FLV_Splitter.IsInstalled ? string.Format("{0} | {1} | {2}", cc.FLV_Splitter.Name, cc.FLV_Splitter.Version, cc.FLV_Splitter.CodecFile) : "";

            chkMP4SplitterInstalled.Checked = cc.MPC_HC_MP4Splitter.IsInstalled;
            tbxMP4Splitter.Text = cc.MPC_HC_MP4Splitter.IsInstalled ? string.Format("{0} | {1} | {2}", cc.MPC_HC_MP4Splitter.Name, cc.MPC_HC_MP4Splitter.Version, cc.MPC_HC_MP4Splitter.CodecFile) : "";

            if (!chkMP4SplitterInstalled.Checked)
            {
                chkMP4SplitterInstalled.Checked = cc.HaaliMediaSplitter.IsInstalled;
                tbxMP4Splitter.Text = cc.HaaliMediaSplitter.IsInstalled ? string.Format("{0} | {1} | {2}", cc.HaaliMediaSplitter.Name, cc.HaaliMediaSplitter.Version, cc.HaaliMediaSplitter.CodecFile) : "";
            }

            chkWMVSplitterInstalled.Checked = cc.WM_ASFReader.IsInstalled;
            tbxWMVSplitter.Text = cc.WM_ASFReader.IsInstalled ? string.Format("{0} | {1} | {2}", cc.WM_ASFReader.Name, cc.WM_ASFReader.Version, cc.WM_ASFReader.CodecFile) : "";

            chkAVISplitterInstalled.Checked = cc.AVI_Splitter.IsInstalled;
            tbxAVISplitter.Text = cc.AVI_Splitter.IsInstalled ? string.Format("{0} | {1} | {2}", cc.AVI_Splitter.Name, cc.AVI_Splitter.Version, cc.AVI_Splitter.CodecFile) : "";
        }

        private void btnAddSite_Click(object sender, EventArgs e)
        {
            SiteSettings site = new SiteSettings();
            site.Name = "New";
            site.UtilName = "GenericSite";
            site.IsEnabled = true;
            bindingSourceSiteSettings.Add(site);
            siteList.SelectedItem = site;
            txtSiteName.Focus();
        }

        private void btnDeleteSite_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            bindingSourceSiteSettings.Remove(site);
        }

        private void btnSiteUp_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
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
            SiteSettings site = siteList.SelectedItem as SiteSettings;
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
                        foreach (SiteSettings site in sitesFromDlg) OnlineVideoSettings.Instance.SiteSettingsList.Add(site);
                        if (sitesFromDlg.Count > 0) siteList.SelectedItem = sitesFromDlg[sitesFromDlg.Count - 1];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void AddConfigurationValues(Sites.SiteUtilBase siteUtil, SiteSettings siteSettings)
        {
            // find and set all configuration fields that are not default

            // 1. build a list of all the Fields that are used for OnlineVideosConfiguration
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            foreach (FieldInfo field in siteUtil.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (attrs.Length > 0 && ((CategoryAttribute)attrs[0]).Category == "OnlineVideosConfiguration")
                {
                    fieldInfos.Add(field);
                }
            }

            // 2. get a "clean" site by creating it with empty SiteSettings
            siteSettings.Configuration = new StringHash();
            Sites.SiteUtilBase cleanSiteUtil = SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings);

            // 3. compare and collect different settings
            foreach (FieldInfo field in fieldInfos)
            {
                object defaultValue = field.GetValue(cleanSiteUtil);
                object newValue = field.GetValue(siteUtil);
                if (defaultValue != newValue)
                {
                    // seems that if default value = false, and newvalue = false defaultvalue != newvalue returns true
                    // so added extra check
                    if (defaultValue == null || !defaultValue.Equals(newValue))
                        siteSettings.Configuration.Add(field.Name, newValue.ToString());
                }
            }
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            SiteSettings siteSettings = (SiteSettings)bindingSourceSiteSettings.Current;
            Sites.SiteUtilBase siteUtil = SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings);

            ConfigurationAdvanced ca = new ConfigurationAdvanced();
            ca.Text += " - " + siteSettings.UtilName;
            ca.propertyGrid.SelectedObject = siteUtil;
            if (ca.ShowDialog() == DialogResult.OK)
                AddConfigurationValues(siteUtil, siteSettings);
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

        private void btnPublishSite_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
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
            if (System.IO.File.Exists(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + site.Name + ".png"))
                icon = System.IO.File.ReadAllBytes(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + site.Name + ".png");
            byte[] banner = null;
            if (System.IO.File.Exists(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\" + site.Name + ".png"))
                banner = System.IO.File.ReadAllBytes(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\" + site.Name + ".png");
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
                                    MessageBox.Show(msg, success ? "Success" : "Error", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
                success = ws.SubmitSite(PluginConfiguration.Instance.email, PluginConfiguration.Instance.password, siteXmlString, icon, banner, dll, out msg);
                MessageBox.Show(msg, success ? "Success" : "Error", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // if the site was not submitted, restore old last updated date, so saving won't write the wrong value
            if (!success) site.LastUpdated = lastUdpBkp;
        }

        private void btnReportSite_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            SubmitSiteReport ssrFrm = new SubmitSiteReport() { SiteName = site.Name };
            ssrFrm.ShowDialog();
        }

        OnlineVideosWebservice.Site[] onlineSites = null;
        private void btnImportGlobal_Click(object sender, EventArgs e)
        {
            if (onlineSites == null)
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                onlineSites = ws.GetSitesOverview();
            }

            Dictionary<string, SiteSettings> hashedLocalSites = new Dictionary<string, SiteSettings>();
            foreach (SiteSettings ss in OnlineVideoSettings.Instance.SiteSettingsList) hashedLocalSites.Add(ss.Name, ss);

            List<OnlineVideosWebservice.Site> onlyOnlineSites = new List<OnlineVideos.OnlineVideosWebservice.Site>();

            int i = 0;
            while (i < onlineSites.Length)
            {
                if (!hashedLocalSites.ContainsKey(onlineSites[i].Name))
                {
                    onlineSites[i].Owner_FK = onlineSites[i].Owner_FK.Substring(0, onlineSites[i].Owner_FK.IndexOf('@'));
                    onlyOnlineSites.Add(onlineSites[i]);
                }
                i++;
            }
            ImportGlobalSite frm = new ImportGlobalSite();
            frm.dgvSites.DataSource = onlyOnlineSites;
            if (frm.ShowDialog() == DialogResult.OK)
            {
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
                            foreach (SiteSettings site in sitesFromServer) OnlineVideoSettings.Instance.SiteSettingsList.Add(site);
                        }
                        byte[] icon = ws.GetSiteIcon(onlineSite.Name);
                        if (icon != null && icon.Length > 0)
                        {
                            System.IO.File.WriteAllBytes(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + onlineSite.Name + ".png", icon);
                        }
                        icon = ws.GetSiteBanner(onlineSite.Name);
                        if (icon != null && icon.Length > 0)
                        {
                            System.IO.File.WriteAllBytes(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\" + onlineSite.Name + ".png", icon);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = oldCursor;
                }
            }
        }
    }
}
