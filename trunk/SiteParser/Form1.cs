using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Diagnostics;
using OnlineVideos;
using OnlineVideos.Sites;

namespace SiteParser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            generic = new MySiteUtil();
            generic.Initialize(new SiteSettings());
            generic.Name = "please fill";
            generic.Description = "please fill";
            generic.Language = "please fill";
            generic.Settings.UtilName = "GenericSite";
            foreach (PlayerType pt in Enum.GetValues(typeof(PlayerType)))
                playerComboBox.Items.Add(pt);
            playerComboBox.SelectedIndex = 0;

            UtilToGui(generic);
        }

        System.ComponentModel.BindingList<SiteSettings> SiteSettingsList;
        MySiteUtil generic;
        List<RssLink> staticList = new List<RssLink>();


        private void UtilToGui(MySiteUtil util)
        {
            nameTextBox.Text = util.Name;
            BaseUrlTextbox.Text = util.BaseUrl;
            descriptionTextBox.Text = util.Description;
            playerComboBox.SelectedIndex = playerComboBox.Items.IndexOf(util.Player);
            ageCheckBox.Checked = util.AgeCheck;
            languageTextBox.Text = util.Language;

            CategoryRegexTextbox.Text = util.DynamicCategoriesRegEx;
            dynamicCategoryUrlFormatTextBox.Text = util.DynamicCategoryUrlFormatString;
            dynamicCategoryUrlDecodingCheckBox.Checked = util.DynamicCategoryUrlDecoding;

            SubcategorieRegexTextBox.Text = util.DynamicSubCategoriesRegEx;
            SubcategorieUrlFormatTextBox.Text = util.DynamicSubCategoryUrlFormatString;
            dynamicSubCategoryUrlDecodingCheckBox.Checked = util.DynamicSubCategoryUrlDecoding;

            videoListRegexTextBox.Text = util.VideoListRegEx;
            videoListRegexFormatTextBox.Text = util.VideoListRegExFormatString;

            videoThumbFormatStringTextBox.Text = util.VideoThumbFormatString;

            nextPageRegExTextBox.Text = util.NextPageRegEx;
            nextPageRegExUrlFormatStringTextBox.Text = util.NextPageRegExUrlFormatString;
            nextPageRegExUrlDecodingCheckBox.Checked = util.NextPageRegExUrlDecoding;

            videoUrlRegExTextBox.Text = util.VideoUrlRegEx;
            videoUrlFormatStringTextBox.Text = util.VideoUrlFormatString;
            videoListUrlDecodingCheckBox.Checked = util.VideoListUrlDecoding;
            videoUrlDecodingCheckBox.Checked = util.VideoUrlDecoding;

            playlistUrlRegexTextBox.Text = util.PlaylistUrlRegEx;
            playlistUrlFormatStringTextBox.Text = util.PlaylistUrlFormatString;

            fileUrlRegexTextBox.Text = util.FileUrlRegEx;
            fileUrlFormatStringTextBox.Text = util.FileUrlFormatString;
            fileUrlPostStringTextBox.Text = util.FileUrlPostString;
            getRedirectedFileUrlCheckBox.Checked = util.GetRedirectedFileUrl;
            resolveHosterCheckBox.Checked = util.ResolveHoster;

            treeView1.Nodes.Clear();
            TreeNode root = treeView1.Nodes.Add("site");
            foreach (Category cat in staticList)
            {
                root.Nodes.Add(cat.Name).Tag = cat;
                cat.HasSubCategories = true;
            }

        }

        private void GuiToUtil(MySiteUtil util)
        {
            util.Name = nameTextBox.Text;
            util.BaseUrl = BaseUrlTextbox.Text;
            util.Description = descriptionTextBox.Text;
            util.Player = (PlayerType)playerComboBox.SelectedItem;
            util.AgeCheck = ageCheckBox.Checked;
            util.Language = languageTextBox.Text;

            util.DynamicCategoriesRegEx = CategoryRegexTextbox.Text;
            util.DynamicCategoryUrlFormatString = dynamicCategoryUrlFormatTextBox.Text;
            util.DynamicCategoryUrlDecoding = dynamicCategoryUrlDecodingCheckBox.Checked;

            util.DynamicSubCategoriesRegEx = SubcategorieRegexTextBox.Text;
            util.DynamicSubCategoryUrlFormatString = SubcategorieUrlFormatTextBox.Text;
            util.DynamicSubCategoryUrlDecoding = dynamicSubCategoryUrlDecodingCheckBox.Checked;

            util.VideoListRegEx = videoListRegexTextBox.Text;
            util.VideoListRegExFormatString = videoListRegexFormatTextBox.Text;

            util.VideoThumbFormatString = videoThumbFormatStringTextBox.Text;

            util.NextPageRegEx = nextPageRegExTextBox.Text;
            util.NextPageRegExUrlFormatString = nextPageRegExUrlFormatStringTextBox.Text;
            util.NextPageRegExUrlDecoding = nextPageRegExUrlDecodingCheckBox.Checked;

            util.VideoUrlRegEx = videoUrlRegExTextBox.Text;
            util.VideoUrlFormatString = videoUrlFormatStringTextBox.Text;
            util.VideoListUrlDecoding = videoListUrlDecodingCheckBox.Checked;
            util.VideoUrlDecoding = videoUrlDecodingCheckBox.Checked;

            util.PlaylistUrlRegEx = playlistUrlRegexTextBox.Text;
            util.PlaylistUrlFormatString = playlistUrlFormatStringTextBox.Text;

            util.FileUrlRegEx = fileUrlRegexTextBox.Text;
            util.FileUrlFormatString = fileUrlFormatStringTextBox.Text;
            util.FileUrlPostString = fileUrlPostStringTextBox.Text;
            util.GetRedirectedFileUrl = getRedirectedFileUrlCheckBox.Checked;
            util.ResolveHoster = resolveHosterCheckBox.Checked;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode selected = treeView1.SelectedNode;
            if (selected.Tag is Category)
                ShowCategoryInfo((Category)selected.Tag);
            if (selected.Tag is VideoInfo)
                ShowVideoInfo((VideoInfo)selected.Tag);

        }

        object GetTreeViewSelectedNode()
        {
            TreeNode selected = treeView1.SelectedNode;
            if (selected == null)
            {
                MessageBox.Show("nothing selected");
                return null;
            }
            return selected.Tag;
        }

        private void ShowCategoryInfo(Category cat)
        {
            categoryInfoListView.Items.Clear();
            categoryInfoListView.Items.Add("Name").SubItems.Add(cat.Name);
            categoryInfoListView.Items.Add("Url").SubItems.Add(((RssLink)cat).Url);
            categoryInfoListView.Items.Add("Thumb").SubItems.Add(cat.Thumb);
            categoryInfoListView.Items.Add("Descr").SubItems.Add(cat.Description);
        }

        private void ShowVideoInfo(VideoInfo video)
        {
            categoryInfoListView.Items.Clear();
            categoryInfoListView.Items.Add("Title").SubItems.Add(video.Title);
            categoryInfoListView.Items.Add("VideoUrl").SubItems.Add(video.VideoUrl);
            categoryInfoListView.Items.Add("ImageUrl").SubItems.Add(video.ImageUrl);
            categoryInfoListView.Items.Add("Descr").SubItems.Add(video.Description);
            categoryInfoListView.Items.Add("Length").SubItems.Add(video.Length);
        }

        #region BaseUrl
        private void BaseUrlTextbox_TextChanged(object sender, EventArgs e)
        {
            generic.BaseUrl = ((TextBox)sender).Text;
        }
        #endregion

        #region Category
        private void CreateCategoryRegexButton_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            CategoryRegexTextbox.Text = f2.Execute(CategoryRegexTextbox.Text, BaseUrlTextbox.Text,
                new string[] { "url", "title", "thumb", "description" });
        }

        private void GetCategoriesButton_Click(object sender, EventArgs e)
        {
            //get categories
            GuiToUtil(generic);
            generic.Settings.Categories.Clear();
            foreach (Category cat in staticList)
                generic.Settings.Categories.Add(cat);

            if (generic.DynamicCategoriesRegEx != null)
                generic.DiscoverDynamicCategories();
            treeView1.Nodes.Clear();
            TreeNode root = treeView1.Nodes.Add("site");
            foreach (Category cat in generic.Settings.Categories)
            {
                root.Nodes.Add(cat.Name).Tag = cat;
                cat.HasSubCategories = true;
            }

        }

        private void manageStaticCategoriesButton_Click(object sender, EventArgs e)
        {
            Form3 f3 = new Form3();
            staticList = f3.Execute(staticList);
            GetCategoriesButton_Click(sender, e);
        }

        #endregion

        #region SubCategories
        private void CreateSubcategoriesRegexButton_Click(object sender, EventArgs e)
        {
            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                Form2 f2 = new Form2();
                SubcategorieRegexTextBox.Text = f2.Execute(SubcategorieRegexTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "url", "title", "thumb", "description" });
            }
            else
                MessageBox.Show("no valid category selected");
        }

        private void GetSubCategoriesButton_Click(object sender, EventArgs e)
        {
            //subcategories
            GuiToUtil(generic);

            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null && parentCat.HasSubCategories)
            {
                TreeNode selected = treeView1.SelectedNode;
                selected.Nodes.Clear();
                generic.DiscoverSubCategories(parentCat);
                foreach (Category cat in parentCat.SubCategories)
                {
                    selected.Nodes.Add(cat.Name).Tag = cat;
                    cat.HasSubCategories = false;
                }
            }
            else
                MessageBox.Show("no valid category selected");
        }

        private void manageStaticSubCategoriesButton_Click(object sender, EventArgs e)
        {

            Form3 f3 = new Form3();
            RssLink parentCat = GetTreeViewSelectedNode() as RssLink;
            if (parentCat != null && staticList.Contains(parentCat))
            {
                List<RssLink> subcats = new List<RssLink>();
                foreach (RssLink tmp in parentCat.SubCategories)
                    subcats.Add(tmp);
                parentCat.SubCategories = new List<Category>(f3.Execute(subcats).ToArray());
                GetSubCategoriesButton_Click(sender, e);
            }
            else
                MessageBox.Show("no valid (static) category selected");
        }

        #endregion

        #region VideoList
        private void CreateVideoListRegexButton_Click(object sender, EventArgs e)
        {
            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                Form2 f2 = new Form2();
                videoListRegexTextBox.Text = f2.Execute(videoListRegexTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "Title", "VideoUrl", "ImageUrl", "Description", "Duration", "Airdate" });
            }
            else
                MessageBox.Show("no valid category selected");
        }

        private void GetVideoListButton_Click(object sender, EventArgs e)
        {
            //videolist
            GuiToUtil(generic);

            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                TreeNode selected = treeView1.SelectedNode;
                selected.Nodes.Clear();
                List<VideoInfo> videos = generic.getVideoList(parentCat);
                foreach (VideoInfo video in videos)
                    selected.Nodes.Add(video.Title).Tag = video;
                selected.Text += ' ' + selected.Nodes.Count.ToString();
                if (generic.HasNextPage)
                    nextPageLabel.Text = generic.NextPageUrl;
                else
                    nextPageLabel.Text = String.Empty;
            }
            else
                MessageBox.Show("no valid category selected");
        }
        #endregion

        #region NextPrevPage
        private void CreateNextPageRegexButton_Click(object sender, EventArgs e)
        {
            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                Form2 f2 = new Form2();
                nextPageRegExTextBox.Text = f2.Execute(nextPageRegExTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "url" });
            }
            else
                MessageBox.Show("no valid category selected");
        }

        #endregion

        #region VideoUrl
        private void CreateVideoUrlRegexButton_Click(object sender, EventArgs e)
        {
            VideoInfo video = GetTreeViewSelectedNode() as VideoInfo;
            if (video != null)
            {
                Form2 f2 = new Form2();
                videoUrlRegExTextBox.Text = f2.Execute(videoUrlRegExTextBox.Text, video.VideoUrl, null,
                    new string[] { "m0", "m1", "m2" });
            }
            else
                MessageBox.Show("no valid video selected");
        }

        private void GetVideoUrlButton_Click(object sender, EventArgs e)
        {
            //VideoUrl
            GuiToUtil(generic);

            VideoInfo video = GetTreeViewSelectedNode() as VideoInfo;
            if (video != null)
                videoUrlResultTextBox.Text = generic.getFormattedVideoUrl(video);
            else
                MessageBox.Show("no valid video selected");
        }

        private void CreatePlayListRegexButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(videoUrlResultTextBox.Text))
                MessageBox.Show("VideoUrlResult is empty");
            else
            {
                Form2 f2 = new Form2();
                playlistUrlRegexTextBox.Text = f2.Execute(playlistUrlRegexTextBox.Text, videoUrlResultTextBox.Text,
                    new string[] { "url" });
            }
        }

        private void GetPlayListUrlButton_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            if (String.IsNullOrEmpty(videoUrlResultTextBox.Text))
                MessageBox.Show("VideoUrlResult is empty");
            else
                playListUrlResultTextBox.Text = generic.getPlaylistUrl(videoUrlResultTextBox.Text);
        }

        private void CreateFileUrlRegexButton_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            if (String.IsNullOrEmpty(playListUrlResultTextBox.Text))
                MessageBox.Show("PlaylistUrlResult is empty");
            else
            {
                string webData;
                if (String.IsNullOrEmpty(generic.FileUrlPostString))
                    webData = SiteUtilBase.GetWebData(playListUrlResultTextBox.Text);
                else
                    webData = SiteUtilBase.GetWebDataFromPost(playListUrlResultTextBox.Text, generic.FileUrlPostString);

                Form2 f2 = new Form2();
                fileUrlRegexTextBox.Text = f2.Execute(fileUrlRegexTextBox.Text, webData, playListUrlResultTextBox.Text,
                    new string[] { "m0", "m1", "m2" });
            }
        }

        private void getFileUrlButton_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            if (String.IsNullOrEmpty(playListUrlResultTextBox.Text))
                MessageBox.Show("PlaylistUrlResult is empty");
            else
            {
                Dictionary<string, string> playList = generic.GetPlaybackOptions(playListUrlResultTextBox.Text);
                ResultUrlComboBox.Items.Clear();

                if (playList != null)
                    foreach (string item in playList.Values)
                    {
                        if (generic.GetRedirectedFileUrl)
                            ResultUrlComboBox.Items.Add(SiteUtilBase.GetRedirectedUrl(item));
                        else
                            ResultUrlComboBox.Items.Add(item);
                    }
                if (ResultUrlComboBox.Items.Count > 0)
                    ResultUrlComboBox.SelectedIndex = 0;
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Media Player\\wmplayer.exe"),
                ResultUrlComboBox.SelectedItem as string);
        }

        private void copyUrl_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ResultUrlComboBox.SelectedItem as string);
        }

        private void checkValid_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"""" + ResultUrlComboBox.SelectedItem as string + @""" is " +
                (!Uri.IsWellFormedUriString(ResultUrlComboBox.SelectedItem as string, UriKind.Absolute) ? "NOT " : String.Empty) +
                "valid");
        }

        #endregion

        private void loadSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(SerializableSettings));
                    SerializableSettings s = (SerializableSettings)ser.Deserialize(fs);
                    fs.Close();
                    SiteSettingsList = s.Sites;
                    int i = 0;
                    while (i < SiteSettingsList.Count)
                    {
                        if (SiteSettingsList[i].UtilName != "GenericSite" || SiteSettingsList[i].Configuration == null)
                            SiteSettingsList.RemoveAt(i);
                        else
                            i++;
                    }
                    comboBoxSites.ComboBox.DisplayMember = "Name";
                    comboBoxSites.ComboBox.DataSource = SiteSettingsList;
                }
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Load settings from selected site into TextBoxes
            SiteSettings siteSettings = comboBoxSites.SelectedItem as SiteSettings;
            if (siteSettings != null)
            {
                generic = new MySiteUtil();
                generic.Initialize(siteSettings);
                staticList = new List<RssLink>();
                foreach (RssLink cat in generic.Settings.Categories)
                    staticList.Add(cat);

                UtilToGui(generic);
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            generic.Settings.Categories.Clear();
            foreach (Category cat in staticList)
                generic.Settings.Categories.Add(cat);
            Utils.AddConfigurationValues(generic, generic.Settings);

            XmlSerializer serializer = new XmlSerializer(typeof(SiteSettings));
            XmlDocument doc = new XmlDocument();
            XPathNavigator nav = doc.CreateNavigator();
            XmlWriter writer = nav.AppendChild();
            writer.WriteStartDocument();
            serializer.Serialize(writer, generic.Settings);
            writer.Close();

            XmlNode final = doc.CreateNode(XmlNodeType.Element, "Site", String.Empty);
            foreach (XmlNode node in doc.SelectNodes("//item"))
            {
                if (String.IsNullOrEmpty(node.InnerText))
                    node.ParentNode.RemoveChild(node);
            }

            XmlSerializer ser = new XmlSerializer(typeof(SiteSettings));
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.OmitXmlDeclaration = true;
            using (XmlWriter ww = XmlWriter.Create(sb, xmlSettings))
            {
                doc.WriteContentTo(ww);
            }
            Clipboard.SetText(sb.ToString().Replace(@" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""", String.Empty
                ).Replace(@" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""", String.Empty));// damn namespaces

        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"http://code.google.com/p/mp-onlinevideos2/wiki/SiteParser");
        }

    }

    class MySiteUtil : GenericSiteUtil
    {
        public string BaseUrl { get { return baseUrl; } set { baseUrl = value; } }
        public string Name { get { return Settings.Name; } set { Settings.Name = value; } }
        public string Description { get { return Settings.Description; } set { Settings.Description = value; } }
        public PlayerType Player { get { return Settings.Player; } set { Settings.Player = value; } }
        public bool AgeCheck { get { return Settings.ConfirmAge; } set { Settings.ConfirmAge = value; } }
        public string Language { get { return Settings.Language; } set { Settings.Language = value; } }

        public string DynamicCategoriesRegEx { get { return GetRegex(regEx_dynamicCategories); } set { regEx_dynamicCategories = CreateRegex(value); dynamicCategoriesRegEx = value; } }
        public string DynamicCategoryUrlFormatString { get { return dynamicCategoryUrlFormatString; } set { dynamicCategoryUrlFormatString = value; } }
        public bool DynamicCategoryUrlDecoding { get { return dynamicCategoryUrlDecoding; } set { dynamicCategoryUrlDecoding = value; } }

        public string DynamicSubCategoriesRegEx { get { return GetRegex(regEx_dynamicSubCategories); } set { regEx_dynamicSubCategories = CreateRegex(value); dynamicSubCategoriesRegEx = value; } }
        public string DynamicSubCategoryUrlFormatString { get { return dynamicSubCategoryUrlFormatString; } set { dynamicSubCategoryUrlFormatString = value; } }
        public bool DynamicSubCategoryUrlDecoding { get { return dynamicSubCategoryUrlDecoding; } set { dynamicSubCategoryUrlDecoding = value; } }

        public string VideoListRegEx { get { return GetRegex(regEx_VideoList); } set { regEx_VideoList = CreateRegex(value); videoListRegEx = value; } }
        public string VideoListRegExFormatString { get { return videoListRegExFormatString; } set { videoListRegExFormatString = value; } }
        public bool VideoListUrlDecoding { get { return videoListUrlDecoding; } set { videoListUrlDecoding = value; } }
        public string VideoThumbFormatString { get { return videoThumbFormatString; } set { videoThumbFormatString = value; } }

        public string NextPageRegEx { get { return GetRegex(regEx_NextPage); } set { regEx_NextPage = CreateRegex(value); nextPageRegEx = value; } }
        public string NextPageRegExUrlFormatString { get { return nextPageRegExUrlFormatString; } set { nextPageRegExUrlFormatString = value; } }
        public bool NextPageRegExUrlDecoding { get { return nextPageRegExUrlDecoding; } set { nextPageRegExUrlDecoding = value; } }
        public string NextPageUrl { get { return nextPageUrl; } }

        public string VideoUrlRegEx { get { return GetRegex(regEx_VideoUrl); } set { regEx_VideoUrl = CreateRegex(value); videoUrlRegEx = value; } }
        public string VideoUrlFormatString { get { return videoUrlFormatString; } set { videoUrlFormatString = value; } }
        public bool VideoUrlDecoding { get { return videoUrlDecoding; } set { videoUrlDecoding = value; } }

        public string PlaylistUrlRegEx { get { return GetRegex(regEx_PlaylistUrl); } set { regEx_PlaylistUrl = CreateRegex(value); playlistUrlRegEx = value; } }
        public string PlaylistUrlFormatString { get { return playlistUrlFormatString; } set { playlistUrlFormatString = value; } }
        public string FileUrlRegEx { get { return GetRegex(regEx_FileUrl); } set { regEx_FileUrl = CreateRegex(value); fileUrlRegEx = value; } }
        public string FileUrlFormatString { get { return fileUrlFormatString; } set { fileUrlFormatString = value; } }
        public string FileUrlPostString { get { return fileUrlPostString; } set { fileUrlPostString = value; } }
        public bool GetRedirectedFileUrl { get { return getRedirectedFileUrl; } set { getRedirectedFileUrl = value; } }
        public bool ResolveHoster { get { return resolveHoster; } set { resolveHoster = value; } }

        #region Regex_String
        private Regex CreateRegex(string s)
        {
            if (String.IsNullOrEmpty(s))
                return null;
            return new Regex(s, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        private string GetRegex(Regex r)
        {
            if (r == null) return String.Empty;
            return r.ToString().TrimStart('{').TrimEnd('}');
        }
        #endregion
    }

}
