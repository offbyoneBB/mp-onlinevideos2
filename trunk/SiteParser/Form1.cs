using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
            generic = new GenericSiteUtil();
            generic.Initialize(new SiteSettings());
            generic.Settings.Name = "please fill";
            generic.Settings.Description = "please fill";
            generic.Settings.Language = "en";
            generic.Settings.UtilName = "GenericSite";
            foreach (PlayerType pt in Enum.GetValues(typeof(PlayerType)))
                playerComboBox.Items.Add(pt);
            foreach (GenericSiteUtil.HosterResolving pt in Enum.GetValues(typeof(GenericSiteUtil.HosterResolving)))
                comboBoxResolving.Items.Add(pt);
            playerComboBox.SelectedIndex = 0;

            FillDecodingCombo(categoryUrlDecodingComboBox);
            FillDecodingCombo(subCategoryUrlDecodingComboBox);
            FillDecodingCombo(nextPageUrlDecodingComboBox);
            FillDecodingCombo(videoListUrlDecodingComboBox);
            FillDecodingCombo(videoUrlDecodingComboBox);
            FillDecodingCombo(fileUrlDecodingComboBox);
            FillDecodingCombo(fileUrlNameDecodingComboBox);

            FillLanguagesComboBox();

            UtilToGui(generic);
#if !DEBUG
            debugToolStripMenuItem.Visible = false;
#endif
        }

        System.ComponentModel.BindingList<SiteSettings> SiteSettingsList;
        GenericSiteUtil generic;
        List<RssLink> staticList = new List<RssLink>();

        private void FillDecodingCombo(ComboBox decodingCombo)
        {
            foreach (GenericSiteUtil.UrlDecoding ud in Enum.GetValues(typeof(GenericSiteUtil.UrlDecoding)))
                decodingCombo.Items.Add(ud);
            decodingCombo.SelectedIndex = 0;
        }

        void FillLanguagesComboBox()
        {
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
                dict.Add(lang, GetLanguageInUserLocale(lang));
            }
            dict = dict.OrderBy(d => d.Value).ToDictionary(d => d.Key, d => d.Value);
            cbLanguages.DataSource = new BindingSource(dict, null);
            cbLanguages.DisplayMember = "Value";
            cbLanguages.ValueMember = "Key";
        }

        string GetLanguageInUserLocale(string aLang)
        {
            string name = aLang;
            try
            {
                name = aLang != "--" ? System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag(aLang).DisplayName : "Global";
            }
            catch
            {
                var temp = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures).FirstOrDefault(
                    ci => ci.IetfLanguageTag == aLang || ci.ThreeLetterISOLanguageName == aLang || ci.TwoLetterISOLanguageName == aLang || ci.ThreeLetterWindowsLanguageName == aLang);
                if (temp != null)
                {
                    name = temp.DisplayName;
                }
            }
            return name;
        }

        private void UtilToGui(GenericSiteUtil util)
        {
            nameTextBox.Text = util.Settings.Name;

            baseUrlTextbox.Text = (string)GetProperty(util, "baseUrl");
            descriptionTextBox.Text = util.Settings.Description;
            playerComboBox.SelectedIndex = playerComboBox.Items.IndexOf(util.Settings.Player);
            ageCheckBox.Checked = util.Settings.ConfirmAge;
            forceUtf8CheckBox.Checked = GetForceUTF8();
            string cookieString = (string)GetProperty(util, "cookies");
            if (!String.IsNullOrEmpty(cookieString))
            {
                string[] cookies = cookieString.Split(',');
                cookiesTextBox.Text = String.Join(Environment.NewLine, cookies);
            }
            else
                cookiesTextBox.Text = String.Empty;
            cbLanguages.SelectedValue = util.Settings.Language;

            categoryRegexTextbox.Text = GetRegex(util, "regEx_dynamicCategories");
            categoryUrlFormatTextBox.Text = (string)GetProperty(util, "dynamicCategoryUrlFormatString");
            categoryUrlDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "dynamicCategoryUrlDecoding");
            categoryNextPageRegexTextBox.Text = (string)GetRegex(util, "regEx_dynamicCategoriesNextPage");

            subcategoryRegexTextBox.Text = GetRegex(util, "regEx_dynamicSubCategories");
            subcategoryUrlFormatTextBox.Text = (string)GetProperty(util, "dynamicSubCategoryUrlFormatString");
            subCategoryUrlDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "dynamicSubCategoryUrlDecoding");
            subcategoryNextPageRegexTextBox.Text = (string)GetRegex(util, "regEx_dynamicSubCategoriesNextPage");

            videoListRegexTextBox.Text = GetRegex(util, "regEx_VideoList");
            videoListRegexFormatTextBox.Text = (string)GetProperty(util, "videoListRegExFormatString");

            videoThumbFormatStringTextBox.Text = (string)GetProperty(util, "videoThumbFormatString");

            nextPageRegExTextBox.Text = GetRegex(util, "regEx_NextPage");
            nextPageRegExUrlFormatStringTextBox.Text = (string)GetProperty(util, "nextPageRegExUrlFormatString");
            nextPageUrlDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "nextPageRegExUrlDecoding");

            videoUrlRegExTextBox.Text = GetRegex(util, "regEx_VideoUrl");
            videoUrlFormatStringTextBox.Text = (string)GetProperty(util, "videoUrlFormatString");
            videoListUrlDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "videoListUrlDecoding");
            videoUrlDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "videoUrlDecoding");

            searchUrlTextBox.Text = GetProperty(util, "searchUrl") as string;
            searchPostStringTextBox.Text = GetProperty(util, "searchPostString") as string;

            playlistUrlRegexTextBox.Text = GetRegex(util, "regEx_PlaylistUrl");
            playlistUrlFormatStringTextBox.Text = (string)GetProperty(util, "playlistUrlFormatString");

            fileUrlRegexTextBox.Text = GetRegex(util, "regEx_FileUrl");
            fileUrlFormatStringTextBox.Text = (string)GetProperty(util, "fileUrlFormatString");
            fileUrlDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "fileUrlDecoding");
            fileUrlPostStringTextBox.Text = (string)GetProperty(util, "fileUrlPostString");
            fileUrlNameFormatStringTextBox.Text = (string)GetProperty(util, "fileUrlNameFormatString");
            fileUrlNameDecodingComboBox.SelectedItem = (GenericSiteUtil.UrlDecoding)GetProperty(util, "fileUrlNameDecoding");
            getRedirectedFileUrlCheckBox.Checked = (bool)GetProperty(util, "getRedirectedFileUrl");
            comboBoxResolving.SelectedItem = (GenericSiteUtil.HosterResolving)GetProperty(util, "resolveHoster");

            treeView1.Nodes.Clear();
            TreeNode root = treeView1.Nodes.Add("site");
            foreach (Category cat in staticList)
            {
                root.Nodes.Add(cat.Name).Tag = cat;
                cat.HasSubCategories = true;
            }
            if (root.Nodes.Count > 0)
                root.Expand();
        }

        private void GuiToUtil(GenericSiteUtil util)
        {
            util.Settings.Name = nameTextBox.Text;
            SetProperty(util, "baseUrl", baseUrlTextbox.Text);
            util.Settings.Description = descriptionTextBox.Text;
            util.Settings.Player = (PlayerType)playerComboBox.SelectedItem;
            util.Settings.ConfirmAge = ageCheckBox.Checked;
            SetProperty(util, "forceUTF8Encoding", forceUtf8CheckBox.Checked);
            string[] cookies = cookiesTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            SetProperty(util, "cookies", String.Join(",", cookies));

            util.Settings.Language = cbLanguages.SelectedValue.ToString();

            SetRegex(util, "regEx_dynamicCategories", "dynamicCategoriesRegEx", categoryRegexTextbox.Text);
            SetProperty(util, "dynamicCategoryUrlFormatString", categoryUrlFormatTextBox.Text);
            SetProperty(util, "dynamicCategoryUrlDecoding", categoryUrlDecodingComboBox.SelectedItem);
            SetRegex(util, "regEx_dynamicCategoriesNextPage", "dynamicCategoriesNextPageRegEx", categoryNextPageRegexTextBox.Text);

            SetRegex(util, "regEx_dynamicSubCategories", "dynamicSubCategoriesRegEx", subcategoryRegexTextBox.Text);
            SetProperty(util, "dynamicSubCategoryUrlFormatString", subcategoryUrlFormatTextBox.Text);
            SetProperty(util, "dynamicSubCategoryUrlDecoding", subCategoryUrlDecodingComboBox.SelectedItem);
            SetRegex(util, "regEx_dynamicSubCategoriesNextPage", "dynamicSubCategoriesNextPageRegEx", subcategoryNextPageRegexTextBox.Text);

            SetRegex(util, "regEx_VideoList", "videoListRegEx", videoListRegexTextBox.Text);
            SetProperty(util, "videoListRegExFormatString", videoListRegexFormatTextBox.Text);

            SetProperty(util, "videoThumbFormatString", videoThumbFormatStringTextBox.Text);

            SetRegex(util, "regEx_NextPage", "nextPageRegEx", nextPageRegExTextBox.Text);
            SetProperty(util, "nextPageRegExUrlFormatString", nextPageRegExUrlFormatStringTextBox.Text);
            SetProperty(util, "nextPageRegExUrlDecoding", nextPageUrlDecodingComboBox.SelectedItem);

            SetRegex(util, "regEx_VideoUrl", "videoUrlRegEx", videoUrlRegExTextBox.Text);
            SetProperty(util, "videoUrlFormatString", videoUrlFormatStringTextBox.Text);
            SetProperty(util, "videoListUrlDecoding", videoListUrlDecodingComboBox.SelectedItem);
            SetProperty(util, "videoUrlDecoding", videoUrlDecodingComboBox.SelectedItem);

            SetProperty(util, "searchUrl", searchUrlTextBox.Text);
            SetProperty(util, "searchPostString", searchPostStringTextBox.Text);

            SetRegex(util, "regEx_PlaylistUrl", "playlistUrlRegEx", playlistUrlRegexTextBox.Text);
            SetProperty(util, "playlistUrlFormatString", playlistUrlFormatStringTextBox.Text);

            SetRegex(util, "regEx_FileUrl", "fileUrlRegEx", fileUrlRegexTextBox.Text);
            SetProperty(util, "fileUrlDecoding", fileUrlDecodingComboBox.SelectedItem);
            SetProperty(util, "fileUrlFormatString", fileUrlFormatStringTextBox.Text);
            SetProperty(util, "fileUrlPostString", fileUrlPostStringTextBox.Text);
            SetProperty(util, "fileUrlNameFormatString", fileUrlNameFormatStringTextBox.Text);
            SetProperty(util, "fileUrlNameDecoding", fileUrlNameDecodingComboBox.SelectedItem);
            SetProperty(util, "getRedirectedFileUrl", getRedirectedFileUrlCheckBox.Checked);
            SetProperty(util, "resolveHoster", comboBoxResolving.SelectedItem);
        }

        private string F2Execute(string regexString, string url, string[] names, bool cleanupValues)
        {
            Form2 f2 = new Form2();
            return f2.Execute(regexString, url, names, cleanupValues, GetForceUTF8(), GetCookieContainer());
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
            categoryInfoListView.Items.Add("AirDate").SubItems.Add(video.Airdate);
            categoryInfoListView.Items.Add("Length").SubItems.Add(video.Length);
        }

        #region BaseUrl
        private void BaseUrlTextbox_TextChanged(object sender, EventArgs e)
        {
            SetProperty(generic, "baseUrl", ((TextBox)sender).Text);
        }
        #endregion

        #region Category
        private void CreateCategoryRegexButton_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            categoryRegexTextbox.Text = F2Execute(categoryRegexTextbox.Text, baseUrlTextbox.Text,
                new string[] { "url", "title", "thumb", "description" }, true);
        }

        private void GetCategoriesButton_Click(object sender, EventArgs e)
        {
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;

                //get categories
                GuiToUtil(generic);

                TreeNode selected = treeView1.SelectedNode;
                if (selected != null && selected.Tag is NextPageCategory && selected.Parent.Parent == null)
                {
                    generic.Settings.Categories.RemoveAt(generic.Settings.Categories.Count - 1);
                    generic.DiscoverNextPageCategories((NextPageCategory)selected.Tag);
                }
                else
                {
                    generic.Settings.Categories.Clear();
                    foreach (Category cat in staticList)
                        generic.Settings.Categories.Add(cat);

                    if (GetRegex(generic, "regEx_dynamicCategories") != null)
                        generic.DiscoverDynamicCategories();
                }
                treeView1.Nodes.Clear();
                TreeNode root = treeView1.Nodes.Add("site");
                foreach (Category cat in generic.Settings.Categories)
                {
                    root.Nodes.Add(cat.Name).Tag = cat;
                    cat.HasSubCategories = true;
                }
                root.Expand();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error getting Categories", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = oldCursor;
            }
        }

        private void CreateCategoryNextPageRegexButton_Click(object sender, EventArgs e)
        {
            string baseUrl = (string)GetProperty(generic, "baseUrl");
            if (!String.IsNullOrEmpty(baseUrl))
            {
                categoryNextPageRegexTextBox.Text = F2Execute(categoryNextPageRegexTextBox.Text, baseUrl,
                    new string[] { "url" }, false);
            }
            else
                MessageBox.Show("No BaseUrl specified");

        }

        private void manageStaticCategoriesButton_Click(object sender, EventArgs e)
        {
            Form3 f3 = new Form3();
            staticList = f3.Execute(staticList);
            GetCategoriesButton_Click(sender, e);
        }

        private void makeStaticButton_Click(object sender, EventArgs e)
        {
            foreach (Category cat in generic.Settings.Categories)
                staticList.Add(cat as RssLink);
        }

        #endregion

        #region SubCategories
        private void CreateSubcategoriesRegexButton_Click(object sender, EventArgs e)
        {
            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                subcategoryRegexTextBox.Text = F2Execute(subcategoryRegexTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "url", "title", "thumb", "description" }, true);
            }
            else
                MessageBox.Show("no valid category selected");
        }

        private void GetSubCategoriesButton_Click(object sender, EventArgs e)
        {
            //subcategories
            GuiToUtil(generic);

            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null && (parentCat.HasSubCategories || parentCat is NextPageCategory))
            {
                TreeNode selected = treeView1.SelectedNode;
                if (parentCat is NextPageCategory)
                {
                    selected = selected.Parent;
                    selected.Nodes.RemoveAt(selected.Nodes.Count - 1);
                }
                else
                    selected.Nodes.Clear();
                generic.DiscoverSubCategories(parentCat);
                foreach (Category cat in parentCat.SubCategories)
                {
                    selected.Nodes.Add(cat.Name).Tag = cat;
                    cat.HasSubCategories = false;
                }
                selected.Expand();
            }
            else
                MessageBox.Show("no valid category selected");
        }

        private void CreateSubcategoriesNextPageRegexButton_Click(object sender, EventArgs e)
        {
            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                subcategoryNextPageRegexTextBox.Text = F2Execute(subcategoryNextPageRegexTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "url" }, false);
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
                videoListRegexTextBox.Text = F2Execute(videoListRegexTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "Title", "VideoUrl", "ImageUrl", "Description", "Duration", "Airdate" }, true);
            }
            else
            {
                string searchUrl = GetTreeViewSelectedNode() as String;
                if (searchUrl != null)
                {
                    videoListRegexTextBox.Text = F2Execute(videoListRegexTextBox.Text, searchUrl,
                        new string[] { "Title", "VideoUrl", "ImageUrl", "Description", "Duration", "Airdate" }, true);
                }
                else
                    MessageBox.Show("no valid category selected");
            }
        }

        private void GetVideoListButton_Click(object sender, EventArgs e)
        {
            //videolist
            GuiToUtil(generic);

            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                List<VideoInfo> videos = null;
                TreeNode selected = treeView1.SelectedNode;
                string nodeTitle = parentCat.Name;
                if (parentCat is NextPageVideoCategory)
                {
                    selected = selected.Parent;
                    nodeTitle = selected.Tag as string;
                    selected.Nodes.RemoveAt(selected.Nodes.Count - 1);
                    videos = generic.GetNextPageVideos();
                }
                else
                {
                    selected.Nodes.Clear();
                    videos = generic.GetVideos(parentCat);
                }
                foreach (VideoInfo video in videos)
                {
                    video.CleanDescriptionAndTitle();
                    selected.Nodes.Add(video.Title).Tag = video;
                }
                selected.Text = string.Format("{0} ({1})", nodeTitle, selected.Nodes.Count);

                if (generic.HasNextPage)
                {
                    NextPageVideoCategory npCat = new NextPageVideoCategory();
                    npCat.Url = (string)GetProperty(generic, "nextPageUrl");
                    selected.Nodes.Add(npCat.Name).Tag = npCat;
                }
            }
            else
                MessageBox.Show("no valid category selected");
        }

        private void GetSearchResultsButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SearchQueryTextBox.Text))
            {
                MessageBox.Show("You must enter a search term", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            if (string.IsNullOrEmpty(searchUrlTextBox.Text))
            {
                MessageBox.Show("You must enter an URL for searching", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            GuiToUtil(generic);

            List<ISearchResultItem> videos = generic.Search(SearchQueryTextBox.Text);

            TreeNode node = new TreeNode(string.Format("Search for '{0}' ({1})", SearchQueryTextBox.Text, videos.Count));
            node.Tag = string.Format(searchUrlTextBox.Text, SearchQueryTextBox.Text);
            foreach (VideoInfo video in videos)
            {
                video.CleanDescriptionAndTitle();
                node.Nodes.Add(video.Title).Tag = video;
            }
            if (generic.HasNextPage)
            {
                NextPageVideoCategory npCat = new NextPageVideoCategory();
                npCat.Url = (string)GetProperty(generic, "nextPageUrl");
                node.Nodes.Add(npCat.Name).Tag = npCat;
            }

            treeView1.Nodes[0].Nodes.Add(node);
            treeView1.Nodes[0].Expand();
            treeView1.SelectedNode = node;
        }
        #endregion

        #region NextPrevPage
        private void CreateNextPageRegexButton_Click(object sender, EventArgs e)
        {
            Category parentCat = GetTreeViewSelectedNode() as Category;
            if (parentCat != null)
            {
                nextPageRegExTextBox.Text = F2Execute(nextPageRegExTextBox.Text, ((RssLink)parentCat).Url,
                    new string[] { "url" }, false);
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
                    new string[] { "m0", "m1", "m2" }, false);
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
                playlistUrlRegexTextBox.Text = F2Execute(playlistUrlRegexTextBox.Text, videoUrlResultTextBox.Text,
                    new string[] { "url" }, false);
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
                string post = (string)GetProperty(generic, "fileUrlPostString");
                if (String.IsNullOrEmpty(post))
                    webData = WebCache.Instance.GetWebData(playListUrlResultTextBox.Text, forceUTF8: GetForceUTF8(),
                            cookies: GetCookieContainer());
                else
                    webData = WebCache.Instance.GetWebData(playListUrlResultTextBox.Text, post, forceUTF8: GetForceUTF8(),
                            cookies: GetCookieContainer());

                Form2 f2 = new Form2();
                fileUrlRegexTextBox.Text = f2.Execute(fileUrlRegexTextBox.Text, webData, playListUrlResultTextBox.Text,
                    new string[] { "m0", "m1", "m2", "n0", "n1", "n2" }, false);
            }
        }

        private void getFileUrlButton_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            if (CheckFileUrlRegex())
            {

                if (String.IsNullOrEmpty(playListUrlResultTextBox.Text))
                    MessageBox.Show("PlaylistUrlResult is empty");
                else
                {
                    Dictionary<string, string> playList;
                    if (!String.IsNullOrEmpty(GetRegex(generic, "regEx_FileUrl")))
                        playList = generic.GetPlaybackOptions(playListUrlResultTextBox.Text);
                    else
                    {
                        playList = new Dictionary<string, string>();
                        playList.Add("url", playListUrlResultTextBox.Text);
                    }
                    ResultUrlComboBox.Items.Clear();

                    if (playList != null)
                        foreach (KeyValuePair<string, string> entry in playList)
                        {
                            PlaybackOption po = new PlaybackOption(entry);
                            if ((bool)GetProperty(generic, "getRedirectedFileUrl"))
                                po.Url = WebCache.Instance.GetRedirectedUrl(po.Url);
                            ResultUrlComboBox.Items.Add(po);
                        }

                    if (ResultUrlComboBox.Items.Count > 0)
                        ResultUrlComboBox.SelectedIndex = 0;
                }
            }
        }

        private bool CheckFileUrlRegex()
        {
            if (String.IsNullOrEmpty(GetRegex(generic, "regEx_FileUrl")) && !String.IsNullOrEmpty(GetRegex(generic, "regEx_PlaylistUrl")))
            {
                MessageBox.Show("FileUrlRegex must be filled if PlaylistUrlRegex is used");
                return false;
            }
            return true;
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (ResultUrlComboBox.SelectedItem as PlaybackOption == null)
                MessageBox.Show("Please select the Url to play", "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                System.Diagnostics.Process.Start(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Media Player\\wmplayer.exe"),
                        (ResultUrlComboBox.SelectedItem as PlaybackOption).Url);
        }

        private void copyUrl_Click(object sender, EventArgs e)
        {
            Clipboard.SetText((ResultUrlComboBox.SelectedItem as PlaybackOption).Url);
        }

        private void checkValid_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"""" + (ResultUrlComboBox.SelectedItem as PlaybackOption).Url + @""" is " +
                (!Utils.IsValidUri((ResultUrlComboBox.SelectedItem as PlaybackOption).Url) ? "NOT " : String.Empty) +
                "valid");
        }

        #endregion

        private void loadSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // set the startup directory to the default MediaPortal data directory
            string commonAppData =
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                    + @"\Team MediaPortal\MediaPortal\";
            if (Directory.Exists(commonAppData))
            {
                openFileDialog1.InitialDirectory = commonAppData;
            }

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
                generic = new GenericSiteUtil();
                generic.Initialize(siteSettings);
                staticList = new List<RssLink>();
                foreach (RssLink cat in generic.Settings.Categories)
                    staticList.Add(cat);

                UtilToGui(generic);

                LoadIconAndBanner();
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GuiToUtil(generic);
            generic.Settings.IsEnabled = true;
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

            foreach (XmlNode node in doc.SelectNodes("//SubCategories"))
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
            string res = sb.ToString();
            res = res.Replace(@" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""", String.Empty);
            res = res.Replace(@" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""", String.Empty);// damn namespaces
            res = res.Replace("SiteSettings", "Site");
            Clipboard.SetText(res);

        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"http://code.google.com/p/mp-onlinevideos2/wiki/SiteParser");
        }

        private void regularExpressionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"http://www.regular-expressions.info/");
        }

        private void copyValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (categoryInfoListView.SelectedItems.Count != 1) return;
            ListViewItem.ListViewSubItemCollection item = categoryInfoListView.SelectedItems[0].SubItems;
            Clipboard.SetText(item[1].Text);
        }

        #region PlaybackOption
        private class PlaybackOption
        {
            public PlaybackOption(KeyValuePair<string, string> val)
            {
                Name = val.Key;
                Url = val.Value;
            }
            public string Name;
            public string Url;
            public override string ToString()
            {
                return (String.IsNullOrEmpty(Name) ? String.Empty : Name + " | ") + Url;
            }
        }

        #endregion

        #region debug

        private void f2Exec(string[] names)
        {
            if (String.IsNullOrEmpty(baseUrlTextbox.Text))
            {
                if (MessageBox.Show("Use html data from clipboard?", "No BaseUrl specified", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Form2 f2 = new Form2();
                    f2.Execute(String.Empty, Clipboard.GetText(), null, names, true);
                }
            }
            else
                F2Execute(String.Empty, baseUrlTextbox.Text, names, true);
        }
        private void categoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            f2Exec(new string[] { "url", "title", "thumb", "description" });
        }

        private void videoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            f2Exec(new string[] { "Title", "VideoUrl", "ImageUrl", "Description", "Duration", "Airdate" });
        }

        #endregion

        #region GenericProperties
        private void SetProperty(GenericSiteUtil site, string propertyName, object value)
        {
            typeof(GenericSiteUtil).InvokeMember(propertyName, BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.SetField, null, site, new[] { value });
        }

        private object GetProperty(GenericSiteUtil site, string propertyName)
        {
            return typeof(GenericSiteUtil).InvokeMember(propertyName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField, null, site, null);
        }

        private string GetRegex(GenericSiteUtil site, string regexName)
        {
            Regex r = (Regex)GetProperty(site, regexName);
            if (r == null) return String.Empty;
            return r.ToString().TrimStart('{').TrimEnd('}');
        }

        private void SetRegex(GenericSiteUtil site, string regexName, string propertyName, string value)
        {
            Regex r;
            if (String.IsNullOrEmpty(value))
                r = null;
            else
                r = new Regex(value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            SetProperty(site, regexName, r);
            SetProperty(site, propertyName, value);
        }

        private bool GetForceUTF8()
        {
            return (bool)GetProperty(generic, "forceUTF8Encoding");
        }

        private CookieContainer GetCookieContainer()
        {
            MethodInfo methodInfo = typeof(GenericSiteUtil).GetMethod("GetCookie", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
                return (CookieContainer)methodInfo.Invoke(generic, null);
            return null;
        }
        #endregion

        private class NextPageVideoCategory : RssLink
        {
            public NextPageVideoCategory()
            {
                Name = Translation.Instance.NextPage;
            }
        }

        void LoadIconAndBanner()
        {
            pictureBoxSiteIcon.ImageLocation = null;
            pictureBoxSiteBanner.ImageLocation = null;
            if (!string.IsNullOrEmpty(nameTextBox.Text))
            {
                string ovThumbDir =
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                        + @"\Team MediaPortal\MediaPortal\thumbs\OnlineVideos\";
                if (Directory.Exists(ovThumbDir + "Icons"))
                {
                    string fileName = string.Format(@"{0}{1}\{2}.png", ovThumbDir, "Icons", nameTextBox.Text);
                    if (File.Exists(fileName))
                        pictureBoxSiteIcon.ImageLocation = fileName;
                }
                if (Directory.Exists(ovThumbDir + "Banners"))
                {
                    string fileName = string.Format(@"{0}{1}\{2}.png", ovThumbDir, "Banners", nameTextBox.Text);
                    if (File.Exists(fileName))
                        pictureBoxSiteBanner.ImageLocation = fileName;
                }
            }
        }

        private void pictureBoxSiteIcon_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(nameTextBox.Text) || nameTextBox.Text == "please fill")
            {
                MessageBox.Show("Please set the Site's name first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                openPngDialog.Title = "Chose a a square PNG as Icon for " + nameTextBox.Text;
                if (openPngDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBoxSiteIcon.ImageLocation = openPngDialog.FileName;

                    string ovIconsDir =
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                        + @"\Team MediaPortal\MediaPortal\thumbs\OnlineVideos\Icons";
                    if (!Directory.Exists(ovIconsDir)) Directory.CreateDirectory(ovIconsDir);
                    File.Copy(openPngDialog.FileName, Path.Combine(ovIconsDir, nameTextBox.Text + ".png"), true);
                }
            }
        }

        private void pictureBoxSiteBanner_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(nameTextBox.Text) || nameTextBox.Text == "please fill")
            {
                MessageBox.Show("Please set the Site's name first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                openPngDialog.Title = "Chose a PNG with 3:1 aspect ratio as Banner for " + nameTextBox.Text;
                if (openPngDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBoxSiteBanner.ImageLocation = openPngDialog.FileName;

                    string ovBannersDir =
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                        + @"\Team MediaPortal\MediaPortal\thumbs\OnlineVideos\Banners";
                    if (!Directory.Exists(ovBannersDir)) Directory.CreateDirectory(ovBannersDir);
                    File.Copy(openPngDialog.FileName, Path.Combine(ovBannersDir, nameTextBox.Text + ".png"), true);
                }
            }
        }
    }
}
