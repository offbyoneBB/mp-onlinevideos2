using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MediaPortal.GUI.Library;

namespace OnlineVideos
{
	/// <summary>
	/// Description of Configuration.
	/// </summary>
	public partial class Configuration : Form
	{        
		private string msSelectedCategoryName;
        private int miSelectedCategoryIndex = -1;

		private List<SiteSettings> moSiteList = new List<SiteSettings>();
        
		public Configuration()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();			
		}

		public void Configuration_Load(object sender, EventArgs e)
        {
            SetInfosFromCodecs();

            lblVersion.Text = "Version: " + new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName).Version.ToString();

            foreach (string site in SiteUtilFactory.GetAllNames())
            {
                cbSiteUtil.Items.Add(site);
            }
            
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
            tbxScreenName.Text = settings.BasicHomeScreenName;
			txtThumbLoc.Text = settings.msThumbLocation;
            txtDownloadDir.Text = settings.msDownloadDir;
			String lsFilterList = "";
			String [] lsFilterArray = settings.msFilterArray;

			if(lsFilterArray!=null){
				foreach (String lsFilter in lsFilterArray){
					lsFilterList+=lsFilter+",";
				}
				txtFilters.Text = lsFilterList;
			}            
			foreach(SiteSettings site in settings.moSiteList.Values){
				siteList.Items.Add(site.Name);
				moSiteList.Add(site);
			}
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
            this.chkUseAgeConfirmation.Checked = settings.useAgeConfirmation;
            chkUseAgeConfirmation_CheckedChanged(chkUseAgeConfirmation, EventArgs.Empty);
            cmbYoutubeQuality.SelectedIndex = (int)settings.YouTubeQuality;            
            cmbDasErsteQuality.SelectedIndex = (int)settings.DasErsteQuality;
            this.tbxPin.Text = settings.pinAgeConfirmation;

            // fill language identifiers combobox
            List<string> cultureNames = new List<string>();
            foreach (System.Globalization.CultureInfo ci in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.NeutralCultures))
            {
                string name = ci.Name.IndexOf('-') >= 0 ? ci.Name.Substring(0, ci.Name.IndexOf('-')) : ci.Name;
                if (!cultureNames.Contains(name)) cultureNames.Add(name);
            }
            cultureNames.Sort();
            cbLanguages.Items.AddRange(cultureNames.ToArray());
		}
		
		void SiteListSelectedIndexChanged(object sender, EventArgs e)
		{
			if(siteList.SelectedIndex >= 0)
            {
				OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
				SiteSettings site = moSiteList[siteList.SelectedIndex];
				txtSiteName.Text = site.Name;
				cbSiteUtil.SelectedItem = site.UtilName;
				txtUserId.Text = site.Username;
				txtPassword.Text = site.Password;
				chkEnabled.Checked = site.IsEnabled;
				chkAgeConfirm.Checked = site.ConfirmAge;
                tbxSearchUrl.Text = site.SearchUrl;
                cbLanguages.SelectedItem = site.Language;

                chkEnabled.Enabled = true;
                chkAgeConfirm.Enabled = true;
                txtSiteName.Enabled = true;
                cbSiteUtil.Enabled = true;
                txtUserId.Enabled = true;
                txtPassword.Enabled = site.Util.hasLoginSupport();
                tbxSearchUrl.Enabled = true;
                cbLanguages.Enabled = true;
                btnSiteSave.Enabled = true;
                
                btnDeleteRss.Enabled = true;
                btnSaveRss.Enabled = true;
                btnAddRss.Enabled = true;
                
				CategoryList.Items.Clear();
				tvGroups.Nodes.Clear();

                foreach (KeyValuePair<string, Category> aCat in site.Categories)
                {
                    if (aCat.Value is RssLink) CategoryList.Items.Add(aCat.Key);
                    else if (aCat.Value is Group)
                    {
                        TreeNode aGroupNode = new TreeNode(aCat.Key);
                        aGroupNode.Tag = aCat.Value;
                        foreach (Channel aChannel in (aCat.Value as Group).Channels)
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
			CategoryList.SelectedIndex = -1;
			txtRssUrl.Text = "";
			txtRssName.Text = "";
            txtRssThumb.Text = "";
            txtRssUrl.Enabled = false;
            txtRssName.Enabled = false;
            txtRssThumb.Enabled = false;
            btnSaveRss.Enabled = false;

            tbxChannelName.Text = "";
            tbxChannelThumb.Text = "";
            tbxStreamName.Text = "";
            tbxStreamUrl.Text = "";
            tbxChannelName.Enabled = false;
            tbxChannelThumb.Enabled = false;
            tbxStreamName.Enabled = false;
            tbxStreamUrl.Enabled = false;
            btnSaveChannel.Enabled = false;
            btnDeleteChannel.Enabled = false;
            btnAddChannel.Enabled = false;
            btnAddGroup.Enabled = true;
		}
		
		void CategoryListSelectedIndexChanged(object sender, EventArgs e)
		{
            if (CategoryList.SelectedIndex !=miSelectedCategoryIndex && CategoryList.SelectedIndex > -1)
            {                
                SiteSettings site = moSiteList[siteList.SelectedIndex];
                msSelectedCategoryName = CategoryList.SelectedItem.ToString();
                miSelectedCategoryIndex = CategoryList.SelectedIndex;
                Log.Info("Category change site:{0} with selected category of {1}", site.Name, msSelectedCategoryName);
                RssLink link = null;
                link = site.Categories[msSelectedCategoryName] as RssLink;
                txtRssUrl.Text = link.Url;
                txtRssName.Text = link.Name;
                txtRssThumb.Text = link.Thumb;
                txtRssUrl.Enabled = true;
                txtRssName.Enabled = true;
                txtRssThumb.Enabled = true;
                btnSaveRss.Enabled = true;

            }
            if (CategoryList.SelectedIndex == -1)
            {
                txtRssUrl.Text = "";
                txtRssName.Text = "";
                txtRssThumb.Text = "";
                txtRssUrl.Enabled = false;
                txtRssName.Enabled = false;
                txtRssThumb.Enabled = false;
                btnSaveRss.Enabled = false;
            }
		}
		
		void BtnSiteSaveClick(object sender, EventArgs e)
		{
			OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
			SiteSettings site = moSiteList[siteList.SelectedIndex];			
			site.Name = txtSiteName.Text;
			site.Username = txtUserId.Text;
			site.Password = txtPassword.Text;
			site.ConfirmAge = chkAgeConfirm.Checked;
			site.IsEnabled = chkEnabled.Checked;
            site.SearchUrl = tbxSearchUrl.Text;
            site.Language = cbLanguages.SelectedItem.ToString();
			siteList.Items[siteList.SelectedIndex] = site.Name;
		}
		
		void BtnRssSaveClick(object sender, EventArgs e)
		{
            if (CategoryList.SelectedIndex > -1)
            {
                SiteSettings site = moSiteList[siteList.SelectedIndex];
                RssLink link = null;
                link = site.Categories[msSelectedCategoryName] as RssLink;
                site.Categories.Remove(msSelectedCategoryName);
                link.Name = txtRssName.Text;
                link.Url = txtRssUrl.Text;
                link.Thumb = txtRssThumb.Text != "" ? txtRssThumb.Text : null;
                CategoryList.Items[CategoryList.SelectedIndex] = txtRssName.Text;
                site.Categories.Add(link.Name, link);
                CategoryList.SelectedIndex = -1;
            }
		}
		
		void BtnAddClick(object sender, EventArgs e)
		{            
			RssLink link = new RssLink();
			link.Name = "new";
			link.Url = "http://";
			SiteSettings site = moSiteList[siteList.SelectedIndex];
			site.Categories.Add(link.Name,link);
			CategoryList.Items.Add(link.Name);
			CategoryList.SelectedIndex = CategoryList.Items.Count-1;
			txtRssName.Focus();
            msSelectedCategoryName = "new";
		}
		
		
		void BtnDeleteRssClick(object sender, EventArgs e)
		{
			if(CategoryList.SelectedIndex>-1)
            {				
				SiteSettings site = moSiteList[siteList.SelectedIndex];
				msSelectedCategoryName = CategoryList.SelectedItem.ToString();
				site.Categories.Remove(msSelectedCategoryName);
				CategoryList.Items.RemoveAt(CategoryList.SelectedIndex);
				txtRssName.Text = "";
				txtRssUrl.Text = "";
                miSelectedCategoryIndex = -1;
			}			
		}
		
		void ConfigurationFormClosing(object sender, FormClosingEventArgs e)
		{
			OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
			String lsFilter = txtFilters.Text;			
			String [] lsFilterArray = lsFilter.Split(new char[] { ',' });
			settings.msFilterArray = lsFilterArray;
			settings.msThumbLocation = txtThumbLoc.Text;
            settings.BasicHomeScreenName = tbxScreenName.Text;
            Log.Info("OnlineVideo Configuration - download Dir:" + txtDownloadDir.Text);
			settings.msDownloadDir = txtDownloadDir.Text;
            settings.AppleTrailerSize = (Sites.AppleTrailersUtil.VideoQuality)cmbTrailerSize.SelectedItem;            
            settings.YouTubeQuality = (Sites.YouTubeUtil.YoutubeVideoQuality)cmbYoutubeQuality.SelectedIndex;
            settings.DasErsteQuality = (OnlineVideos.Sites.DasErsteMediathekUtil.DasErsteVideoQuality)cmbDasErsteQuality.SelectedIndex;
            settings.useAgeConfirmation = chkUseAgeConfirmation.Checked;
            settings.pinAgeConfirmation = tbxPin.Text;
			settings.moSiteList.Clear();
			foreach(SiteSettings site in moSiteList){
				settings.moSiteList.Add(site.Name,site);
			}
			settings.Save();
		}

        private void btnSave_Click(object sender, EventArgs e)
        {
            Close();
        }
        
        private void btnYahooConfig_Click(object sender, EventArgs e)
        {
            ConfigurationYahoo yahoo = new ConfigurationYahoo();
            yahoo.ShowDialog();
        }

        private void chkUseAgeConfirmation_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseAgeConfirmation.Checked)
                tbxPin.Enabled = true;
            else
                tbxPin.Enabled = false;
        }

        private void tvGroups_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (tvGroups.SelectedNode != null)
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
                    SiteSettings site = moSiteList[siteList.SelectedIndex];
                    Group group = tvGroups.SelectedNode.Tag as Group;
                    site.Categories.Remove(group.Name);
                    group.Name = tbxChannelName.Text;
                    group.Thumb = tbxChannelThumb.Text != "" ? tbxChannelThumb.Text : null;
                    tvGroups.SelectedNode.Text = tbxChannelName.Text;
                    site.Categories.Add(group.Name, group);                    
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
                    SiteSettings site = moSiteList[siteList.SelectedIndex];
                    site.Categories.Remove(group.Name);
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
            SiteSettings site = moSiteList[siteList.SelectedIndex];
            Group g = new Group();
            g.Name = "New";
            site.Categories.Add(g.Name, g);
            TreeNode node = new TreeNode("New");
            node.Tag = g;
            tvGroups.Nodes.Add(node);
        }

        private void btnAddChannel_Click(object sender, EventArgs e)
        {
            Group group = tvGroups.SelectedNode.Tag as Group;
            Channel c = new Channel();
            c.StreamName = "New";
            group.Channels.Add(c);
            TreeNode node = new TreeNode("New");
            node.Tag = c;
            tvGroups.SelectedNode.Nodes.Add(node);
        }

        private void btnBrowseForDlFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDownloadDir.Text = folderBrowserDialog1.SelectedPath;
            }
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

        private void tabGeneral_Click(object sender, EventArgs e)
        {

        }
	}
}
