using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace OnlineVideos
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
		}

		public void Configuration_Load(object sender, EventArgs e)
        {
            /** fill "Codecs" tab **/
            SetInfosFromCodecs();

            /** fill "General" tab **/
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
            lblVersion.Text = "Version: " + new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName).Version.ToString();            
            tbxScreenName.Text = settings.BasicHomeScreenName;
			txtThumbLoc.Text = settings.msThumbLocation;
            txtDownloadDir.Text = settings.msDownloadDir;
            txtFilters.Text = settings.msFilterArray != null ? string.Join(",", settings.msFilterArray) : "";
            chkUseAgeConfirmation.Checked = settings.useAgeConfirmation;
            chkUseAgeConfirmation_CheckedChanged(chkUseAgeConfirmation, EventArgs.Empty);
            tbxPin.Text = settings.pinAgeConfirmation;
            cmbYoutubeQuality.SelectedIndex = (int)settings.YouTubeQuality;
            cmbDasErsteQuality.SelectedIndex = (int)settings.DasErsteQuality;
            // apple trailer quality selection
            foreach (Sites.AppleTrailersUtil.VideoQuality size in Enum.GetValues(typeof(Sites.AppleTrailersUtil.VideoQuality)))
            {
                if (size != OnlineVideos.Sites.AppleTrailersUtil.VideoQuality.UNKNOWN)
                {
                    cmbTrailerSize.Items.Add(size);
                    if (size == OnlineVideoSettings.getInstance().AppleTrailerSize)
                    {
                        cmbTrailerSize.SelectedIndex = cmbTrailerSize.Items.Count - 1;
                    }
                }
			}

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
            cultureNames.Sort();
            cbLanguages.Items.AddRange(cultureNames.ToArray());            

            // set bindings            
            bindingSourceSiteSettings.DataSource = OnlineVideoSettings.getInstance().SiteSettingsList;
		}
		
		void SiteListSelectedIndexChanged(object sender, EventArgs e)
        {            
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            BindingList<RssLink> rssLinks = new BindingList<RssLink>();

            tvGroups_AfterSelect(tvGroups, new TreeViewEventArgs(null, TreeViewAction.Unknown));
            tvGroups.Nodes.Clear();                

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
            }

            bindingSourceRssLink.DataSource = rssLinks;
            RssLinkListSelectedIndexChanged(this, EventArgs.Empty);

            btnAddRss.Enabled = site != null;
            btnAddGroup.Enabled = site != null;

            btnDeleteSite.Enabled = site != null;
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
			if(RssLinkList.SelectedIndex > -1)
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
                OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
                String lsFilter = txtFilters.Text;
                String[] lsFilterArray = lsFilter.Split(new char[] { ',' });
                settings.msFilterArray = lsFilterArray;
                settings.msThumbLocation = txtThumbLoc.Text;
                settings.BasicHomeScreenName = tbxScreenName.Text;                
                settings.msDownloadDir = txtDownloadDir.Text;
                settings.AppleTrailerSize = (Sites.AppleTrailersUtil.VideoQuality)cmbTrailerSize.SelectedItem;
                settings.YouTubeQuality = (Sites.YouTubeUtil.YoutubeVideoQuality)cmbYoutubeQuality.SelectedIndex;
                settings.DasErsteQuality = (OnlineVideos.Sites.DasErsteMediathekUtil.DasErsteVideoQuality)cmbDasErsteQuality.SelectedIndex;
                settings.useAgeConfirmation = chkUseAgeConfirmation.Checked;
                settings.pinAgeConfirmation = tbxPin.Text;
                settings.Save();
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

        private void btnYahooConfig_Click(object sender, EventArgs e)
        {
            ConfigurationYahoo yahoo = new ConfigurationYahoo();
            yahoo.ShowDialog();
        }

        void SetInfosFromCodecs()
        {
            CodecConfiguration cc = OnlineVideoSettings.getInstance().CodecConfiguration;

            chkFLVSplitterInstalled.Checked = cc.MPC_HC_FLVSplitter.IsInstalled;
            tbxFLVSplitter.Text = cc.MPC_HC_FLVSplitter.IsInstalled  ? string.Format("{0} | {1}", cc.MPC_HC_FLVSplitter.CodecFile, cc.MPC_HC_FLVSplitter.Version) : "";

            chkMP4SplitterInstalled.Checked = cc.MPC_HC_MP4Splitter.IsInstalled;
            tbxMP4Splitter.Text = cc.MPC_HC_MP4Splitter.IsInstalled ? string.Format("{0} | {1}", cc.MPC_HC_MP4Splitter.CodecFile, cc.MPC_HC_MP4Splitter.Version) : "";

            if (!chkMP4SplitterInstalled.Checked)
            {
                chkMP4SplitterInstalled.Checked = cc.HaaliMediaSplitter.IsInstalled;
                tbxMP4Splitter.Text = cc.HaaliMediaSplitter.IsInstalled ? string.Format("{0} | {1}", cc.HaaliMediaSplitter.CodecFile, cc.HaaliMediaSplitter.Version) : "";
            }

            chkWMVSplitterInstalled.Checked = cc.WM_ASFReader.IsInstalled;
            tbxWMVSplitter.Text = cc.WM_ASFReader.IsInstalled ? string.Format("{0} | {1}", cc.WM_ASFReader.CodecFile, cc.WM_ASFReader.Version) : "";

            chkAVISplitterInstalled.Checked = cc.AVI_Splitter.IsInstalled;
            tbxAVISplitter.Text = cc.AVI_Splitter.IsInstalled ? string.Format("{0} | {1}", cc.AVI_Splitter.CodecFile, cc.AVI_Splitter.Version) : "";
        }

        private void btnAddSite_Click(object sender, EventArgs e)
        {
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
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
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
            SiteSettings site = siteList.SelectedItem as SiteSettings;            
            bindingSourceSiteSettings.Remove(site);            
        }

        private void btnSiteUp_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            siteList.SelectedIndex = -1;
            bindingSourceSiteSettings.SuspendBinding();

            int currentPos = OnlineVideoSettings.getInstance().SiteSettingsList.IndexOf(site);
            OnlineVideoSettings.getInstance().SiteSettingsList.Remove(site);
            if (currentPos == 0) OnlineVideoSettings.getInstance().SiteSettingsList.Add(site);
            else OnlineVideoSettings.getInstance().SiteSettingsList.Insert(currentPos - 1, site);

            bindingSourceSiteSettings.ResumeBinding();
            bindingSourceSiteSettings.Position = OnlineVideoSettings.getInstance().SiteSettingsList.IndexOf(site); 
            bindingSourceSiteSettings.ResetCurrentItem();
        }

        private void btnSiteDown_Click(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            siteList.SelectedIndex = -1;
            bindingSourceSiteSettings.SuspendBinding();

            int currentPos = OnlineVideoSettings.getInstance().SiteSettingsList.IndexOf(site);
            OnlineVideoSettings.getInstance().SiteSettingsList.Remove(site);
            if (currentPos >= OnlineVideoSettings.getInstance().SiteSettingsList.Count) OnlineVideoSettings.getInstance().SiteSettingsList.Insert(0, site);
            else OnlineVideoSettings.getInstance().SiteSettingsList.Insert(currentPos + 1, site);

            bindingSourceSiteSettings.ResumeBinding();
            bindingSourceSiteSettings.Position = OnlineVideoSettings.getInstance().SiteSettingsList.IndexOf(site);
            bindingSourceSiteSettings.ResetCurrentItem();
        }

        private void btnImportSite_Click(object sender, EventArgs e)
        {
            try
            {
                ImExportXml dialog = new ImExportXml();
                if (dialog.ShowDialog() == DialogResult.OK)
                {                    
                    string xml = dialog.txtXml.Text;
                    xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OnlineVideoSites xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Sites>
" + xml + @"
</Sites>
</OnlineVideoSites>";
                    System.IO.StringReader sr = new System.IO.StringReader(xml);
                    System.Xml.Serialization.XmlSerializer ser = OnlineVideoSettings.getInstance().XmlSerImp.GetSerializer(typeof(SerializableSettings));
                    SerializableSettings s = (SerializableSettings)ser.Deserialize(sr);
                    if (s.Sites != null)
                    {
                        foreach (SiteSettings site in s.Sites) OnlineVideoSettings.getInstance().SiteSettingsList.Add(site);
                        if (s.Sites.Count > 0) siteList.SelectedItem = s.Sites[s.Sites.Count - 1];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        siteSettings.Configuration.Add(field.Name, newValue.ToString());
                    }
                }
            }
        }        
	}
}
