using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using OnlineVideos;

namespace SiteParser
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            categoryListBox_SelectedIndexChanged(this, EventArgs.Empty);
        }

        public List<RssLink> Execute(List<RssLink> src)
        {
            List<RssLink> result = new List<RssLink>();
            foreach (RssLink tmp in src)
            {
                RssLink nw = new RssLink() { Name = tmp.Name, Description = tmp.Description, Url = tmp.Url, Thumb = tmp.Thumb };
                nw.SubCategories = tmp.SubCategories;
                result.Add(nw);
            }
            bindingSourceRssLink.DataSource = result;
            if (ShowDialog() == DialogResult.OK)
                return result;

            return src;
        }

        private void categoryListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            nameTextBox.Enabled = RssLinkList.SelectedIndex != -1;
            urlTextBox.Enabled = RssLinkList.SelectedIndex != -1;
            descriptionTextBox.Enabled = RssLinkList.SelectedIndex != -1;
            thumbTextBox.Enabled = RssLinkList.SelectedIndex != -1;

            btnDeleteRss.Enabled = RssLinkList.SelectedIndex != -1;
        }

        void BtnAddRss_Click(object sender, EventArgs e)
        {
            RssLinkList.SelectedIndex = -1;
            RssLink link = new RssLink() { Name = "new", Url = "http://" };
            ((CurrencyManager)BindingContext[bindingSourceRssLink]).List.Add(link);
            RssLinkList.SelectedIndex = RssLinkList.Items.Count - 1;
            categoryListBox_SelectedIndexChanged(this, EventArgs.Empty);
            nameTextBox.Focus();
        }

        void BtnDeleteRss_Click(object sender, EventArgs e)
        {
            if (RssLinkList.SelectedIndex > -1)
                ((CurrencyManager)BindingContext[bindingSourceRssLink]).RemoveAt(RssLinkList.SelectedIndex);
        }

    }
}
