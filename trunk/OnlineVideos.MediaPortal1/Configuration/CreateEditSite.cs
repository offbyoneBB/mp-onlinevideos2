using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal1
{
    public partial class CreateEditSite : Form
    {
        public CreateEditSite()
        {
            InitializeComponent();

            propertyGridSiteSettings.BrowsableAttributes = new AttributeCollection(new CategoryAttribute("OnlineVideosConfiguration"));

            // utils combobox
            foreach (string site in SiteUtilFactory.GetAllNames()) cbSiteUtil.Items.Add(site);
            
            // language identifiers combobox
            List<string> cultureNames = new List<string>();
            foreach (System.Globalization.CultureInfo ci in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.NeutralCultures))
            {
                if (!string.IsNullOrEmpty(ci.Name))
                {
                    string name = ci.Name.IndexOf('-') >= 0 ? ci.Name.Substring(0, ci.Name.IndexOf('-')) : ci.Name;
                    if (!cultureNames.Contains(name)) cultureNames.Add(name);
                }
            }
            cultureNames.Add("--");
            var dict = new Dictionary<string, string>();
            foreach (string lang in cultureNames)
            {
                dict.Add(lang, PluginConfiguration.GetLanguageInUserLocale(lang));
            }
            dict = dict.OrderBy(d => d.Value).ToDictionary(d => d.Key, d => d.Value);
            cbLanguages.DataSource = new BindingSource(dict, null);
            cbLanguages.DisplayMember = "Value";
            cbLanguages.ValueMember = "Key";
        }

        public Sites.SiteUtilBase SiteUtil { get; private set; }

        private void CreateEditSite_Load(object sender, EventArgs e)
        {
            cbSiteUtil.SelectedItem = (SiteSettingsBindingSource.Current as SiteSettings).UtilName;
            RebuildTreeView();
        }

        private void RebuildTreeView(object select = null)
        {
            tvGroups.Nodes.Clear();
            SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
            if (site.Categories != null)
            {
                foreach (Category aCat in site.Categories)
                {
                    if (aCat is RssLink)
                    {
                        TreeNode aRssNode = new TreeNode(aCat.Name);
                        aRssNode.ImageIndex = 0;
                        aRssNode.SelectedImageIndex = 0;
                        aRssNode.Tag = aCat;
                        tvGroups.Nodes.Add(aRssNode);
                        if (aCat == select) tvGroups.SelectedNode = aRssNode;
                    }
                    else if (aCat is Group)
                    {
                        TreeNode aGroupNode = new TreeNode(aCat.Name);
                        aGroupNode.ImageIndex = 2;
                        aGroupNode.SelectedImageIndex = 2;
                        aGroupNode.Tag = aCat;
                        tvGroups.Nodes.Add(aGroupNode);
                        if (aCat == select) tvGroups.SelectedNode = aGroupNode;
                        if ((aCat as Group).Channels != null)
                        {
                            foreach (Channel aChannel in (aCat as Group).Channels)
                            {
                                TreeNode aChannelNode = new TreeNode(aChannel.StreamName);
                                aChannelNode.ImageIndex = 1;
                                aChannelNode.SelectedImageIndex = 1;
                                aChannelNode.Tag = aChannel;
                                aGroupNode.Nodes.Add(aChannelNode);
                                if (!aGroupNode.IsExpanded) aGroupNode.Expand();
                                if (aChannel == select) tvGroups.SelectedNode = aChannelNode;
                            }
                        }
                    }
                }
                if (tvGroups.SelectedNode != null) tvGroups.SelectedNode.EnsureVisible();
            }
        }

        private void tvGroups_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null)
            {
                btnAddStream.Enabled = false;
                btnDelete.Enabled = false;
                tablessTabControl1.SelectedTab = tabPageEmpty;
            }
            else if (e.Node.Tag is Group)
            {
                btnAddStream.Enabled = true;
                btnDelete.Enabled = true;
                groupBindingSource.DataSource = e.Node.Tag;
                tablessTabControl1.SelectedTab = tabPageGroup;
            }
            else if (e.Node.Tag is Channel)
            {
                btnAddStream.Enabled = false;
                btnDelete.Enabled = true;
                channelBindingSource.DataSource = e.Node.Tag;
                tablessTabControl1.SelectedTab = tabPageChannel;
            }
            else if (e.Node.Tag is RssLink)
            {
                btnAddStream.Enabled = false;
                btnDelete.Enabled = true;
                bindingSourceRssLink.DataSource = e.Node.Tag;
                tablessTabControl1.SelectedTab = tabPageRssLink;
            }
        }

        void SelectedSiteUtilChanged(object sender, EventArgs e)
        {
            if (cbSiteUtil.SelectedIndex != -1)
            {
                SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
                site.UtilName = (string)cbSiteUtil.SelectedItem;
                try
                {
                    propertyGridSiteSettings.SelectedObject = SiteUtilFactory.CreateFromShortName(site.UtilName, site);
                }
                catch
                {
                    propertyGridSiteSettings.SelectedObject = null;
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (tvGroups.SelectedNode == null || tvGroups.SelectedNode.Tag == null) return;
            else if (tvGroups.SelectedNode.Tag is Group)
            {
                (SiteSettingsBindingSource.Current as SiteSettings).Categories.Remove(tvGroups.SelectedNode.Tag as Group);
                RebuildTreeView();
            }
            else if (tvGroups.SelectedNode.Tag is Channel)
            {
                (tvGroups.SelectedNode.Parent.Tag as Group).Channels.Remove(tvGroups.SelectedNode.Tag as Channel);
                RebuildTreeView();
            }
            else if (tvGroups.SelectedNode.Tag is RssLink)
            {
                (SiteSettingsBindingSource.Current as SiteSettings).Categories.Remove(tvGroups.SelectedNode.Tag as RssLink);
                RebuildTreeView();
            }
        }

        private void btnAddGroup_Click(object sender, EventArgs e)
        {
            Group group = new Group() { Name = "New" };
            (SiteSettingsBindingSource.Current as SiteSettings).AddCategoryForSerialization(group);
            RebuildTreeView(group);
        }

        private void btnAddStream_Click(object sender, EventArgs e)
        {
            Group group = tvGroups.SelectedNode.Tag as Group;
            Channel channel = new Channel() { StreamName = "New" };
            if (group.Channels == null) group.Channels = new BindingList<Channel>();
            group.Channels.Add(channel);
            RebuildTreeView(channel);
        }

        private void btnAddRss_Click(object sender, EventArgs e)
        {
            RssLink link = new RssLink() { Name = "new", Url = "http://" };
            (SiteSettingsBindingSource.Current as SiteSettings).AddCategoryForSerialization(link);
            RebuildTreeView(link);
        }
    }
}
