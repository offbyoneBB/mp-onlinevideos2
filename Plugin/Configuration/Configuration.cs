using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos
{
	/// <summary>
	/// Description of Configuration.
	/// </summary>
	public partial class Configuration : Form
	{
        BindingList<SiteSettings> sites = new BindingList<SiteSettings>();

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
            foreach (SiteSettings site in settings.moSiteList.Values) sites.Add(site);
            bindingSourceSite.DataSource = sites;
            siteList.DataSource = bindingSourceSite;
		}
		
		void SiteListSelectedIndexChanged(object sender, EventArgs e)
        {
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            if (site != null)
            {
                CategoryList.SelectedIndex = -1;
                tvGroups_AfterSelect(tvGroups, new TreeViewEventArgs(null, TreeViewAction.Unknown));

                CategoryList.Items.Clear();
                tvGroups.Nodes.Clear();

                foreach (Category aCat in site.CategoriesArray)
                {
                    if (aCat is RssLink) CategoryList.Items.Add(aCat);
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

                btnAddRss.Enabled = true;
                btnAddGroup.Enabled = true;

                btnDeleteSite.Enabled = true;
            }
            else
            {
                btnDeleteSite.Enabled = false;
            }
		}
		
		void CategoryListSelectedIndexChanged(object sender, EventArgs e)
		{
            if (CategoryList.SelectedIndex > -1)
            {                
                RssLink link = CategoryList.SelectedItem as RssLink;
                txtRssUrl.Text = link.Url;
                txtRssName.Text = link.Name;
                txtRssThumb.Text = link.Thumb;
                txtRssUrl.Enabled = true;
                txtRssName.Enabled = true;
                txtRssThumb.Enabled = true;
                btnSaveRss.Enabled = true;
                btnDeleteRss.Enabled = true;
            }
            else
            {
                txtRssUrl.Text = "";
                txtRssName.Text = "";
                txtRssThumb.Text = "";
                txtRssUrl.Enabled = false;
                txtRssName.Enabled = false;
                txtRssThumb.Enabled = false;
                btnSaveRss.Enabled = false;
                btnDeleteRss.Enabled = false;
            }
		}		
		
		void BtnRssSaveClick(object sender, EventArgs e)
		{
            if (CategoryList.SelectedIndex > -1)
            {
                SiteSettings site = siteList.SelectedItem as SiteSettings;
                RssLink link = CategoryList.SelectedItem as RssLink;
                // remove old category from site
                site.Categories.Remove(link.Name);
                // set new properties
                link.Name = txtRssName.Text;
                link.Url = txtRssUrl.Text;
                link.Thumb = txtRssThumb.Text != "" ? txtRssThumb.Text : null;
                // reset the item in the listbox
                CategoryList.Items[CategoryList.SelectedIndex] = link;
                // add new category to the site
                site.Categories.Add(link.Name, link);
                // unselect
                CategoryList.SelectedIndex = -1;
            }
		}
		
		void BtnAddRssClick(object sender, EventArgs e)
		{   
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            if (site.Categories.ContainsKey("new"))
                MessageBox.Show("Please rename existing category with name: new", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                RssLink link = new RssLink();
                link.Name = "new";
                link.Url = "http://";
                site.Categories.Add(link.Name, link);
                CategoryList.Items.Add(link);
                CategoryList.SelectedIndex = CategoryList.Items.Count - 1;
                txtRssName.Focus();
            }
		}
		
		
		void BtnDeleteRssClick(object sender, EventArgs e)
		{
			if(CategoryList.SelectedIndex>-1)
            {
                SiteSettings site = siteList.SelectedItem as SiteSettings;
                RssLink link = CategoryList.SelectedItem as RssLink;
				site.Categories.Remove(link.Name);
				CategoryList.Items.RemoveAt(CategoryList.SelectedIndex);
				txtRssName.Text = "";
				txtRssUrl.Text = "";                
			}			
		}
		
		void ConfigurationFormClosing(object sender, FormClosingEventArgs e)
		{
            if (DialogResult == DialogResult.OK)
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

                settings.moSiteList.Clear();
                foreach (SiteSettings site in sites) settings.moSiteList.Add(site.Name, site);

                settings.Save();
            }
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
                    SiteSettings site = siteList.SelectedItem as SiteSettings;
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
            SiteSettings site = siteList.SelectedItem as SiteSettings;
            if (site.Categories.ContainsKey("New"))
                MessageBox.Show("Please rename existing category with name: New", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                Group g = new Group();
                g.Name = "New";
                site.Categories.Add(g.Name, g);
                TreeNode node = new TreeNode("New");
                node.Tag = g;
                tvGroups.Nodes.Add(node);
                tvGroups.SelectedNode = node;
                tbxChannelName.Focus();
            }
        }

        private void btnAddChannel_Click(object sender, EventArgs e)
        {
            Group group = tvGroups.SelectedNode.Tag as Group;
            Channel c = new Channel();
            c.StreamName = "New";
            if (group.Channels == null) group.Channels = new List<Channel>();
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
            bindingSourceSite.Add(site);
            siteList.SelectedItem = site;
            txtSiteName.Focus();
        }

        private void btnDeleteSite_Click(object sender, EventArgs e)
        {
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
            SiteSettings site = siteList.SelectedItem as SiteSettings;            
            bindingSourceSite.Remove(site);            
        }        
	}
}
