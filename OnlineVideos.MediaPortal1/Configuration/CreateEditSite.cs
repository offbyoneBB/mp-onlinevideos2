using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
            SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
            BindingList<RssLink> rssLinks = new BindingList<RssLink>();

            tvGroups_AfterSelect(tvGroups, new TreeViewEventArgs(null, TreeViewAction.Unknown));
            tvGroups.Nodes.Clear();

            if (site.Categories != null)
            {
                foreach (Category aCat in site.Categories)
                {
                    if (aCat is RssLink) rssLinks.Add(aCat as RssLink);
                    else if (aCat is Group)
                    {
                        TreeNode aGroupNode = new TreeNode(aCat.Name);
                        aGroupNode.Tag = aCat;
                        if ((aCat as Group).Channels != null)
                        {
                            foreach (Channel aChannel in (aCat as Group).Channels)
                            {
                                TreeNode node = new TreeNode(aChannel.StreamName);
                                node.Tag = aChannel;
                                aGroupNode.Nodes.Add(node);
                            }
                        }
                        tvGroups.Nodes.Add(aGroupNode);
                    }
                }
                tvGroups.ExpandAll();
            }

            SiteUtil = SiteUtilFactory.CreateFromShortName(site.UtilName, site);
            propertyGridSiteSettings.SelectedObject = SiteUtil;

            if (SiteUtil == null) cbSiteUtil.SelectedIndex = -1;
            else cbSiteUtil.SelectedItem = site.UtilName;

            bindingSourceRssLink.DataSource = rssLinks;
            RssLinkListSelectedIndexChanged(this, EventArgs.Empty);
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

        void RssLinkListSelectedIndexChanged(object sender, EventArgs e)
        {
            // enable/disable all TextBoxes and Buttons for RssLink if one/none is selected
            txtRssUrl.Enabled = RssLinkList.SelectedIndex > -1;
            txtRssName.Enabled = RssLinkList.SelectedIndex > -1;
            txtRssThumb.Enabled = RssLinkList.SelectedIndex > -1;
            btnDeleteRss.Enabled = RssLinkList.SelectedIndex > -1;
        }

        void SelectedSiteUtilChanged(object sender, EventArgs e)
        {
            if (cbSiteUtil.SelectedIndex != -1)
            {
                (SiteSettingsBindingSource.Current as SiteSettings).UtilName = (string)cbSiteUtil.SelectedItem;
            }
        }

        void BtnAddRssClick(object sender, EventArgs e)
        {
            RssLink link = new RssLink() { Name = "new", Url = "http://" };
            (SiteSettingsBindingSource.Current as SiteSettings).AddCategoryForSerialization(link);
            ((CurrencyManager)BindingContext[bindingSourceRssLink]).List.Add(link);
            RssLinkList.SelectedIndex = RssLinkList.Items.Count - 1;
            RssLinkListSelectedIndexChanged(this, EventArgs.Empty);
            txtRssName.Focus();
        }

        void BtnDeleteRssClick(object sender, EventArgs e)
        {
            if (RssLinkList.SelectedIndex > -1)
            {
                SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
                RssLink link = RssLinkList.SelectedItem as RssLink;
                ((CurrencyManager)BindingContext[bindingSourceRssLink]).RemoveAt(RssLinkList.SelectedIndex);
                site.Categories.Remove(link);
            }
        }

        private void btnSaveChannel_Click(object sender, EventArgs e)
        {
            if (tvGroups.SelectedNode != null)
            {
                if (tvGroups.SelectedNode.Parent == null)
                {
                    SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
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
                    SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
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
            SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
            Group g = new Group();
            g.Name = "New";
            site.AddCategoryForSerialization(g);
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
    }
}
