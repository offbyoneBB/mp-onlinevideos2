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
            foreach (string site in Sites.SiteUtilFactory.GetAllNames()) cbSiteUtil.Items.Add(site);
            
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

        public Sites.SiteUtilBase SiteUtil { get { return propertyGridSiteSettings.SelectedObject as Sites.SiteUtilBase; } }

        private void CreateEditSite_Load(object sender, EventArgs e)
        {
            cbSiteUtil.SelectedItem = (SiteSettingsBindingSource.Current as SiteSettings).UtilName;
            RebuildTreeView();
        }

        void RebuildTreeView(object select = null)
        {
            tvGroups.Nodes.Clear();
            SiteSettings site = SiteSettingsBindingSource.Current as SiteSettings;
            BuildTreeRecursive(site.Categories, null, select);
            if (tvGroups.SelectedNode != null) tvGroups.SelectedNode.EnsureVisible();
            else tvGroups_AfterSelect(null, new TreeViewEventArgs(null));
        }

        void BuildTreeRecursive(IList<Category> categories, DataboundTreeNode parentNode, object select)
        {
            if (categories == null) return;
            foreach (Category category in categories)
            {
                DataboundTreeNode categoryNode = new DataboundTreeNode(category.Name);
                categoryNode.TagPropertyBoundToText = "Name";
                categoryNode.Tag = category;
                if (parentNode == null) tvGroups.Nodes.Add(categoryNode);
                else parentNode.Nodes.Add(categoryNode);
                if (category == select) tvGroups.SelectedNode = categoryNode;
                if (category is Group)
                {
                    categoryNode.ImageIndex = 2;
                    categoryNode.SelectedImageIndex = 2;
                    if ((category as Group).Channels != null)
                    {
                        foreach (Channel aChannel in (category as Group).Channels)
                        {
                            DataboundTreeNode aChannelNode = new DataboundTreeNode(aChannel.StreamName);
                            aChannelNode.TagPropertyBoundToText = "StreamName";
                            aChannelNode.ImageIndex = 1;
                            aChannelNode.SelectedImageIndex = 1;
                            aChannelNode.Tag = aChannel;
                            categoryNode.Nodes.Add(aChannelNode);
                            if (!categoryNode.IsExpanded) categoryNode.Expand();
                            if (aChannel == select) tvGroups.SelectedNode = aChannelNode;
                        }
                    }
                }
                else
                {
                    BuildTreeRecursive(category.SubCategories, categoryNode, select);
                }
            }
        }

        private void tvGroups_AfterSelect(object sender, TreeViewEventArgs e)
        {
            object tag = e.Node as DataboundTreeNode != null ? (e.Node as DataboundTreeNode).Tag : null;

            if (tag == null)
            {
                tvGroups.ContextMenuStrip = null;
                btnAddStream.Enabled = false;
                btnDelete.Enabled = false;
				btnItemUp.Enabled = false;
				btnItemDown.Enabled = false;
                tablessTabControl1.SelectedTab = tabPageEmpty;
            }
            else if (tag is Group)
            {
                tvGroups.ContextMenuStrip = null;
                btnAddStream.Enabled = true;
                btnDelete.Enabled = true;
				btnItemUp.Enabled = true;
				btnItemDown.Enabled = true;
                groupBindingSource.DataSource = tag;
                tablessTabControl1.SelectedTab = tabPageGroup;
            }
            else if (tag is Channel)
            {
                tvGroups.ContextMenuStrip = null;
                btnAddStream.Enabled = false;
                btnDelete.Enabled = true;
				btnItemUp.Enabled = true;
				btnItemDown.Enabled = true;
                channelBindingSource.DataSource = tag;
                tablessTabControl1.SelectedTab = tabPageChannel;
            }
            else if (tag is RssLink)
            {
                tvGroups.ContextMenuStrip = contextMenuTreeView;
                btnAddStream.Enabled = false;
                btnDelete.Enabled = true;
				btnItemUp.Enabled = true;
				btnItemDown.Enabled = true;
                bindingSourceRssLink.DataSource = tag;
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
                    propertyGridSiteSettings.SelectedObject = Sites.SiteUtilFactory.CreateFromShortName(site.UtilName, site);
                }
                catch
                {
                    propertyGridSiteSettings.SelectedObject = null;
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            object tag = tvGroups.SelectedNode as DataboundTreeNode != null ? (tvGroups.SelectedNode as DataboundTreeNode).Tag : null;

            if (tag == null) return;
            else if (tag is Category)
            {
                var parentNode = tvGroups.SelectedNode.Parent as DataboundTreeNode;
                if (parentNode != null)
                    (parentNode.Tag as Category).SubCategories.Remove(tag as Category);
                else
                    (SiteSettingsBindingSource.Current as SiteSettings).Categories.Remove(tag as Category);
                RebuildTreeView();
            }
            else if (tag is Channel)
            {
                ((tvGroups.SelectedNode.Parent as DataboundTreeNode).Tag as Group).Channels.Remove(tag as Channel);
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
            Group group = (tvGroups.SelectedNode as DataboundTreeNode).Tag as Group;
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

        private void addSubcategoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var parentCategory = (tvGroups.SelectedNode as DataboundTreeNode).Tag as RssLink;
            RssLink link = new RssLink() { Name = "new", Url = "http://" };
            parentCategory.AddSubCategoryForSerialization(link);
            RebuildTreeView(link);
        }

        private void btnCreateRtmpLink_Click(object sender, EventArgs e)
        {
            RtmpUrlGenerator r = new RtmpUrlGenerator();
            string url = (!string.IsNullOrEmpty(tbxStreamUrl.Text) && tbxStreamUrl.Text.ToLower().StartsWith("rtmp")) ? tbxStreamUrl.Text : "rtmp://host.domain/app";
            r.propertyGrid1.SelectedObject = MPUrlSourceFilter.RtmpUrl.Parse(url);
            if (r.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbxStreamUrl.Text = ((MPUrlSourceFilter.RtmpUrl)r.propertyGrid1.SelectedObject).ToString();
            }
        }

        private void btnItemUp_Click(object sender, EventArgs e)
        {
			object item = (tvGroups.SelectedNode as DataboundTreeNode).Tag;
			if (item is Category)
			{
				Category c = item as Category;
				var parentNode = tvGroups.SelectedNode.Parent as DataboundTreeNode;
				IList<Category> list;
				if (parentNode != null)
				{
					list = (parentNode.Tag as Category).SubCategories;
				}
				else
				{
					list = (SiteSettingsBindingSource.Current as SiteSettings).Categories;
				}
				int index = list.IndexOf(c);
				list.RemoveAt(index);
				if (index == 0) list.Add(c);
				else list.Insert(index - 1, c);
				RebuildTreeView(c);
			}
			else if (item is Channel)
			{
				Channel c = item as Channel;
				Group g = (tvGroups.SelectedNode.Parent as DataboundTreeNode).Tag as Group;
				int index = g.Channels.IndexOf(c);
				g.Channels.RemoveAt(index);
				if (index == 0) g.Channels.Add(c);
				else g.Channels.Insert(index - 1, c);
				RebuildTreeView(c);
			}
        }

        private void btnItemDown_Click(object sender, EventArgs e)
        {
			object item = (tvGroups.SelectedNode as DataboundTreeNode).Tag;
			if (item is Category)
			{
				Category c = item as Category;
				var parentNode = tvGroups.SelectedNode.Parent as DataboundTreeNode;
				IList<Category> list;
				if (parentNode != null)
				{
					list = (parentNode.Tag as Category).SubCategories;
				}
				else
				{
					list = (SiteSettingsBindingSource.Current as SiteSettings).Categories;
				}
				int index = list.IndexOf(c);
				list.RemoveAt(index);
				if (index >= list.Count) index = -1;
				list.Insert(index + 1, c);
				RebuildTreeView(c);
			}
			else if (item is Channel)
			{
				Channel c = item as Channel;
				Group g = (tvGroups.SelectedNode.Parent as DataboundTreeNode).Tag as Group;
				int index = g.Channels.IndexOf(c);
				g.Channels.RemoveAt(index);
				if (index >= g.Channels.Count) index = -1;
				g.Channels.Insert(index + 1, c);
				RebuildTreeView(c);
			}
        }
    }
}
