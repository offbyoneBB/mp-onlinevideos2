using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OnlineVideos;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Cornerstone;

namespace CornerstoneTest
{
    public partial class Form1 : Form
    {
        ScriptUtil scriptUtil = new ScriptUtil();
        public Form1()
        {
            InitializeComponent();
            scriptUtil.scriptFile = @"f:\devel\scraper\sitesc.xml";
            scriptUtil.Initialize(new SiteSettings());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            list_categories.Items.Clear();
            int i = scriptUtil.DiscoverDynamicCategories();
            foreach (var category in scriptUtil.Settings.Categories)
            {
                list_categories.Items.Add(category);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (list_categories.SelectedItems.Count > 0)
            {
                button2.Enabled = true;
                propertyGrid1.SelectedObject = list_categories.SelectedItem;
            }
            else
            {
                button2.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<VideoInfo> list = scriptUtil.getVideoList(list_categories.SelectedItem as Category);

            list_videos.Items.Clear();

            foreach (var videoInfo in list)
            {
                list_videos.Items.Add(videoInfo);
            }
        }

        private void list_videos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(list_videos.SelectedItems.Count>0)
            {
                propertyGrid2.SelectedObject = list_videos.SelectedItem;   
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (list_videos.SelectedItems.Count > 0)
            {
                textBox1.Text = scriptUtil.getUrl(list_videos.SelectedItem as VideoInfo);
            }
        }
    }
}
