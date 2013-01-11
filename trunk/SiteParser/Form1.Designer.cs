namespace SiteParser
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.generalTabPage = new System.Windows.Forms.TabPage();
			this.nameTextBox = new System.Windows.Forms.TextBox();
			this.label26 = new System.Windows.Forms.Label();
			this.playerComboBox = new System.Windows.Forms.ComboBox();
			this.label25 = new System.Windows.Forms.Label();
			this.descriptionTextBox = new System.Windows.Forms.TextBox();
			this.label24 = new System.Windows.Forms.Label();
			this.cbLanguages = new System.Windows.Forms.ComboBox();
			this.label23 = new System.Windows.Forms.Label();
			this.ageCheckBox = new System.Windows.Forms.CheckBox();
			this.baseUrlTextbox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.categTabPage = new System.Windows.Forms.TabPage();
			this.label27 = new System.Windows.Forms.Label();
			this.categoryUrlDecodingComboBox = new System.Windows.Forms.ComboBox();
			this.CreateCategoryNextPageRegexButton = new System.Windows.Forms.Button();
			this.categoryNextPageRegexTextBox = new System.Windows.Forms.TextBox();
			this.label30 = new System.Windows.Forms.Label();
			this.makeStaticButton = new System.Windows.Forms.Button();
			this.manageStaticCategoriesButton = new System.Windows.Forms.Button();
			this.CreateCategoryRegexButton = new System.Windows.Forms.Button();
			this.categoryUrlFormatTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.GetCategoriesButton = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.categoryRegexTextbox = new System.Windows.Forms.TextBox();
			this.subCatTabPage = new System.Windows.Forms.TabPage();
			this.label29 = new System.Windows.Forms.Label();
			this.subCategoryUrlDecodingComboBox = new System.Windows.Forms.ComboBox();
			this.CreateSubcategoriesNextPageRegexButton = new System.Windows.Forms.Button();
			this.subcategoryNextPageRegexTextBox = new System.Windows.Forms.TextBox();
			this.label28 = new System.Windows.Forms.Label();
			this.manageStaticSubCategoriesButton = new System.Windows.Forms.Button();
			this.CreateSubcategoriesRegexButton = new System.Windows.Forms.Button();
			this.subcategoryUrlFormatTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.GetSubCategoriesButton = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.subcategoryRegexTextBox = new System.Windows.Forms.TextBox();
			this.videoListTabPage = new System.Windows.Forms.TabPage();
			this.GetSearchResultsButton = new System.Windows.Forms.Button();
			this.searchPostStringTextBox = new System.Windows.Forms.TextBox();
			this.label35 = new System.Windows.Forms.Label();
			this.searchUrlTextBox = new System.Windows.Forms.TextBox();
			this.label34 = new System.Windows.Forms.Label();
			this.label32 = new System.Windows.Forms.Label();
			this.nextPageUrlDecodingComboBox = new System.Windows.Forms.ComboBox();
			this.label31 = new System.Windows.Forms.Label();
			this.videoListUrlDecodingComboBox = new System.Windows.Forms.ComboBox();
			this.CreateNextPageRegexButton = new System.Windows.Forms.Button();
			this.CreateVideoListRegexButton = new System.Windows.Forms.Button();
			this.nextPageRegExUrlFormatStringTextBox = new System.Windows.Forms.TextBox();
			this.nextPageRegExTextBox = new System.Windows.Forms.TextBox();
			this.label10 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.videoThumbFormatStringTextBox = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.videoListRegexFormatTextBox = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.GetVideoListButton = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.videoListRegexTextBox = new System.Windows.Forms.TextBox();
			this.VideoUrlTabPage = new System.Windows.Forms.TabPage();
			this.label33 = new System.Windows.Forms.Label();
			this.videoUrlDecodingComboBox = new System.Windows.Forms.ComboBox();
			this.fileUrlNameFormatStringTextBox = new System.Windows.Forms.TextBox();
			this.label12 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.comboBoxResolving = new System.Windows.Forms.ComboBox();
			this.fileUrlPostStringTextBox = new System.Windows.Forms.TextBox();
			this.label18 = new System.Windows.Forms.Label();
			this.getRedirectedFileUrlCheckBox = new System.Windows.Forms.CheckBox();
			this.ResultUrlComboBox = new System.Windows.Forms.ComboBox();
			this.getFileUrlButton = new System.Windows.Forms.Button();
			this.playListUrlResultTextBox = new System.Windows.Forms.TextBox();
			this.label15 = new System.Windows.Forms.Label();
			this.GetPlayListUrlButton = new System.Windows.Forms.Button();
			this.videoUrlResultTextBox = new System.Windows.Forms.TextBox();
			this.label22 = new System.Windows.Forms.Label();
			this.btnPlay = new System.Windows.Forms.Button();
			this.playButtonContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.play = new System.Windows.Forms.ToolStripMenuItem();
			this.copyUrl = new System.Windows.Forms.ToolStripMenuItem();
			this.checkValid = new System.Windows.Forms.ToolStripMenuItem();
			this.label21 = new System.Windows.Forms.Label();
			this.CreateFileUrlRegexButton = new System.Windows.Forms.Button();
			this.fileUrlFormatStringTextBox = new System.Windows.Forms.TextBox();
			this.fileUrlRegexTextBox = new System.Windows.Forms.TextBox();
			this.label19 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.CreatePlayListRegexButton = new System.Windows.Forms.Button();
			this.playlistUrlFormatStringTextBox = new System.Windows.Forms.TextBox();
			this.playlistUrlRegexTextBox = new System.Windows.Forms.TextBox();
			this.label16 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.CreateVideoUrlRegexButton = new System.Windows.Forms.Button();
			this.videoUrlFormatStringTextBox = new System.Windows.Forms.TextBox();
			this.label13 = new System.Windows.Forms.Label();
			this.GetVideoUrlButton = new System.Windows.Forms.Button();
			this.label14 = new System.Windows.Forms.Label();
			this.videoUrlRegExTextBox = new System.Windows.Forms.TextBox();
			this.categoryInfoListView = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.CategoryViewContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copyValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadSitesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.comboBoxSites = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.categoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.videoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.SearchQueryTextBox = new System.Windows.Forms.TextBox();
			this.tabControl1.SuspendLayout();
			this.generalTabPage.SuspendLayout();
			this.categTabPage.SuspendLayout();
			this.subCatTabPage.SuspendLayout();
			this.videoListTabPage.SuspendLayout();
			this.VideoUrlTabPage.SuspendLayout();
			this.playButtonContextMenuStrip.SuspendLayout();
			this.CategoryViewContextMenuStrip.SuspendLayout();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.generalTabPage);
			this.tabControl1.Controls.Add(this.categTabPage);
			this.tabControl1.Controls.Add(this.subCatTabPage);
			this.tabControl1.Controls.Add(this.videoListTabPage);
			this.tabControl1.Controls.Add(this.VideoUrlTabPage);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 30, 3, 3);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(468, 581);
			this.tabControl1.TabIndex = 16;
			// 
			// generalTabPage
			// 
			this.generalTabPage.Controls.Add(this.nameTextBox);
			this.generalTabPage.Controls.Add(this.label26);
			this.generalTabPage.Controls.Add(this.playerComboBox);
			this.generalTabPage.Controls.Add(this.label25);
			this.generalTabPage.Controls.Add(this.descriptionTextBox);
			this.generalTabPage.Controls.Add(this.label24);
			this.generalTabPage.Controls.Add(this.cbLanguages);
			this.generalTabPage.Controls.Add(this.label23);
			this.generalTabPage.Controls.Add(this.ageCheckBox);
			this.generalTabPage.Controls.Add(this.baseUrlTextbox);
			this.generalTabPage.Controls.Add(this.label1);
			this.generalTabPage.Location = new System.Drawing.Point(4, 22);
			this.generalTabPage.Name = "generalTabPage";
			this.generalTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.generalTabPage.Size = new System.Drawing.Size(460, 555);
			this.generalTabPage.TabIndex = 4;
			this.generalTabPage.Text = "General";
			this.generalTabPage.UseVisualStyleBackColor = true;
			// 
			// nameTextBox
			// 
			this.nameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.nameTextBox.Location = new System.Drawing.Point(3, 22);
			this.nameTextBox.Name = "nameTextBox";
			this.nameTextBox.Size = new System.Drawing.Size(451, 20);
			this.nameTextBox.TabIndex = 12;
			// 
			// label26
			// 
			this.label26.AutoSize = true;
			this.label26.Location = new System.Drawing.Point(0, 6);
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size(35, 13);
			this.label26.TabIndex = 11;
			this.label26.Text = "Name";
			// 
			// playerComboBox
			// 
			this.playerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.playerComboBox.FormattingEnabled = true;
			this.playerComboBox.Location = new System.Drawing.Point(3, 146);
			this.playerComboBox.Name = "playerComboBox";
			this.playerComboBox.Size = new System.Drawing.Size(111, 21);
			this.playerComboBox.TabIndex = 10;
			// 
			// label25
			// 
			this.label25.AutoSize = true;
			this.label25.Location = new System.Drawing.Point(0, 130);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(36, 13);
			this.label25.TabIndex = 9;
			this.label25.Text = "Player";
			// 
			// descriptionTextBox
			// 
			this.descriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.descriptionTextBox.Location = new System.Drawing.Point(3, 107);
			this.descriptionTextBox.Name = "descriptionTextBox";
			this.descriptionTextBox.Size = new System.Drawing.Size(451, 20);
			this.descriptionTextBox.TabIndex = 8;
			// 
			// label24
			// 
			this.label24.AutoSize = true;
			this.label24.Location = new System.Drawing.Point(0, 91);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(60, 13);
			this.label24.TabIndex = 7;
			this.label24.Text = "Description";
			// 
			// cbLanguages
			// 
			this.cbLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cbLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbLanguages.Location = new System.Drawing.Point(3, 209);
			this.cbLanguages.Name = "cbLanguages";
			this.cbLanguages.Size = new System.Drawing.Size(451, 21);
			this.cbLanguages.TabIndex = 6;
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Location = new System.Drawing.Point(0, 193);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(55, 13);
			this.label23.TabIndex = 5;
			this.label23.Text = "Language";
			// 
			// ageCheckBox
			// 
			this.ageCheckBox.AutoSize = true;
			this.ageCheckBox.Location = new System.Drawing.Point(3, 173);
			this.ageCheckBox.Name = "ageCheckBox";
			this.ageCheckBox.Size = new System.Drawing.Size(79, 17);
			this.ageCheckBox.TabIndex = 4;
			this.ageCheckBox.Text = "Age Check";
			this.ageCheckBox.UseVisualStyleBackColor = true;
			// 
			// baseUrlTextbox
			// 
			this.baseUrlTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.baseUrlTextbox.Location = new System.Drawing.Point(3, 64);
			this.baseUrlTextbox.Name = "baseUrlTextbox";
			this.baseUrlTextbox.Size = new System.Drawing.Size(451, 20);
			this.baseUrlTextbox.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(0, 48);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "BaseUrl";
			// 
			// categTabPage
			// 
			this.categTabPage.Controls.Add(this.label27);
			this.categTabPage.Controls.Add(this.categoryUrlDecodingComboBox);
			this.categTabPage.Controls.Add(this.CreateCategoryNextPageRegexButton);
			this.categTabPage.Controls.Add(this.categoryNextPageRegexTextBox);
			this.categTabPage.Controls.Add(this.label30);
			this.categTabPage.Controls.Add(this.makeStaticButton);
			this.categTabPage.Controls.Add(this.manageStaticCategoriesButton);
			this.categTabPage.Controls.Add(this.CreateCategoryRegexButton);
			this.categTabPage.Controls.Add(this.categoryUrlFormatTextBox);
			this.categTabPage.Controls.Add(this.label3);
			this.categTabPage.Controls.Add(this.GetCategoriesButton);
			this.categTabPage.Controls.Add(this.label2);
			this.categTabPage.Controls.Add(this.categoryRegexTextbox);
			this.categTabPage.Location = new System.Drawing.Point(4, 22);
			this.categTabPage.Name = "categTabPage";
			this.categTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.categTabPage.Size = new System.Drawing.Size(460, 555);
			this.categTabPage.TabIndex = 0;
			this.categTabPage.Text = "Category";
			this.categTabPage.UseVisualStyleBackColor = true;
			// 
			// label27
			// 
			this.label27.AutoSize = true;
			this.label27.Location = new System.Drawing.Point(0, 175);
			this.label27.Name = "label27";
			this.label27.Size = new System.Drawing.Size(149, 13);
			this.label27.TabIndex = 57;
			this.label27.Text = "DynamicCategoryUrlDecoding";
			// 
			// categoryUrlDecodingComboBox
			// 
			this.categoryUrlDecodingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.categoryUrlDecodingComboBox.FormattingEnabled = true;
			this.categoryUrlDecodingComboBox.Location = new System.Drawing.Point(155, 172);
			this.categoryUrlDecodingComboBox.Name = "categoryUrlDecodingComboBox";
			this.categoryUrlDecodingComboBox.Size = new System.Drawing.Size(66, 21);
			this.categoryUrlDecodingComboBox.TabIndex = 56;
			// 
			// CreateCategoryNextPageRegexButton
			// 
			this.CreateCategoryNextPageRegexButton.Location = new System.Drawing.Point(3, 196);
			this.CreateCategoryNextPageRegexButton.Name = "CreateCategoryNextPageRegexButton";
			this.CreateCategoryNextPageRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateCategoryNextPageRegexButton.TabIndex = 55;
			this.CreateCategoryNextPageRegexButton.Text = "Create Regex";
			this.CreateCategoryNextPageRegexButton.UseVisualStyleBackColor = true;
			this.CreateCategoryNextPageRegexButton.Click += new System.EventHandler(this.CreateCategoryNextPageRegexButton_Click);
			// 
			// categoryNextPageRegexTextBox
			// 
			this.categoryNextPageRegexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.categoryNextPageRegexTextBox.Location = new System.Drawing.Point(3, 238);
			this.categoryNextPageRegexTextBox.Name = "categoryNextPageRegexTextBox";
			this.categoryNextPageRegexTextBox.Size = new System.Drawing.Size(451, 20);
			this.categoryNextPageRegexTextBox.TabIndex = 54;
			// 
			// label30
			// 
			this.label30.AutoSize = true;
			this.label30.Location = new System.Drawing.Point(0, 222);
			this.label30.Name = "label30";
			this.label30.Size = new System.Drawing.Size(86, 13);
			this.label30.TabIndex = 53;
			this.label30.Text = "NextPageRegEx";
			// 
			// makeStaticButton
			// 
			this.makeStaticButton.Location = new System.Drawing.Point(106, 264);
			this.makeStaticButton.Name = "makeStaticButton";
			this.makeStaticButton.Size = new System.Drawing.Size(99, 23);
			this.makeStaticButton.TabIndex = 31;
			this.makeStaticButton.Text = "Dynamic -> Static";
			this.makeStaticButton.UseVisualStyleBackColor = true;
			this.makeStaticButton.Click += new System.EventHandler(this.makeStaticButton_Click);
			// 
			// manageStaticCategoriesButton
			// 
			this.manageStaticCategoriesButton.Location = new System.Drawing.Point(3, 264);
			this.manageStaticCategoriesButton.Name = "manageStaticCategoriesButton";
			this.manageStaticCategoriesButton.Size = new System.Drawing.Size(97, 23);
			this.manageStaticCategoriesButton.TabIndex = 30;
			this.manageStaticCategoriesButton.Text = "Static Categories";
			this.manageStaticCategoriesButton.UseVisualStyleBackColor = true;
			this.manageStaticCategoriesButton.Click += new System.EventHandler(this.manageStaticCategoriesButton_Click);
			// 
			// CreateCategoryRegexButton
			// 
			this.CreateCategoryRegexButton.Location = new System.Drawing.Point(3, 3);
			this.CreateCategoryRegexButton.Name = "CreateCategoryRegexButton";
			this.CreateCategoryRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateCategoryRegexButton.TabIndex = 29;
			this.CreateCategoryRegexButton.Text = "Create Regex";
			this.CreateCategoryRegexButton.UseVisualStyleBackColor = true;
			this.CreateCategoryRegexButton.Click += new System.EventHandler(this.CreateCategoryRegexButton_Click);
			// 
			// categoryUrlFormatTextBox
			// 
			this.categoryUrlFormatTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.categoryUrlFormatTextBox.Location = new System.Drawing.Point(3, 147);
			this.categoryUrlFormatTextBox.Name = "categoryUrlFormatTextBox";
			this.categoryUrlFormatTextBox.Size = new System.Drawing.Size(451, 20);
			this.categoryUrlFormatTextBox.TabIndex = 26;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(0, 131);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(135, 13);
			this.label3.TabIndex = 25;
			this.label3.Text = "DynamicCategoryUrlFormat";
			// 
			// GetCategoriesButton
			// 
			this.GetCategoriesButton.Location = new System.Drawing.Point(89, 3);
			this.GetCategoriesButton.Name = "GetCategoriesButton";
			this.GetCategoriesButton.Size = new System.Drawing.Size(75, 23);
			this.GetCategoriesButton.TabIndex = 23;
			this.GetCategoriesButton.Text = "GetCats";
			this.GetCategoriesButton.UseVisualStyleBackColor = true;
			this.GetCategoriesButton.Click += new System.EventHandler(this.GetCategoriesButton_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(0, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(80, 13);
			this.label2.TabIndex = 21;
			this.label2.Text = "CategoryRegex";
			// 
			// categoryRegexTextbox
			// 
			this.categoryRegexTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.categoryRegexTextbox.Location = new System.Drawing.Point(3, 48);
			this.categoryRegexTextbox.Multiline = true;
			this.categoryRegexTextbox.Name = "categoryRegexTextbox";
			this.categoryRegexTextbox.Size = new System.Drawing.Size(451, 80);
			this.categoryRegexTextbox.TabIndex = 16;
			// 
			// subCatTabPage
			// 
			this.subCatTabPage.Controls.Add(this.label29);
			this.subCatTabPage.Controls.Add(this.subCategoryUrlDecodingComboBox);
			this.subCatTabPage.Controls.Add(this.CreateSubcategoriesNextPageRegexButton);
			this.subCatTabPage.Controls.Add(this.subcategoryNextPageRegexTextBox);
			this.subCatTabPage.Controls.Add(this.label28);
			this.subCatTabPage.Controls.Add(this.manageStaticSubCategoriesButton);
			this.subCatTabPage.Controls.Add(this.CreateSubcategoriesRegexButton);
			this.subCatTabPage.Controls.Add(this.subcategoryUrlFormatTextBox);
			this.subCatTabPage.Controls.Add(this.label4);
			this.subCatTabPage.Controls.Add(this.GetSubCategoriesButton);
			this.subCatTabPage.Controls.Add(this.label5);
			this.subCatTabPage.Controls.Add(this.subcategoryRegexTextBox);
			this.subCatTabPage.Location = new System.Drawing.Point(4, 22);
			this.subCatTabPage.Name = "subCatTabPage";
			this.subCatTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.subCatTabPage.Size = new System.Drawing.Size(460, 555);
			this.subCatTabPage.TabIndex = 1;
			this.subCatTabPage.Text = "Subcategories";
			this.subCatTabPage.UseVisualStyleBackColor = true;
			// 
			// label29
			// 
			this.label29.AutoSize = true;
			this.label29.Location = new System.Drawing.Point(0, 175);
			this.label29.Name = "label29";
			this.label29.Size = new System.Drawing.Size(168, 13);
			this.label29.TabIndex = 59;
			this.label29.Text = "DynamicSubCategoryUrlDecoding";
			// 
			// subCategoryUrlDecodingComboBox
			// 
			this.subCategoryUrlDecodingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.subCategoryUrlDecodingComboBox.FormattingEnabled = true;
			this.subCategoryUrlDecodingComboBox.Location = new System.Drawing.Point(174, 172);
			this.subCategoryUrlDecodingComboBox.Name = "subCategoryUrlDecodingComboBox";
			this.subCategoryUrlDecodingComboBox.Size = new System.Drawing.Size(66, 21);
			this.subCategoryUrlDecodingComboBox.TabIndex = 58;
			// 
			// CreateSubcategoriesNextPageRegexButton
			// 
			this.CreateSubcategoriesNextPageRegexButton.Location = new System.Drawing.Point(3, 196);
			this.CreateSubcategoriesNextPageRegexButton.Name = "CreateSubcategoriesNextPageRegexButton";
			this.CreateSubcategoriesNextPageRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateSubcategoriesNextPageRegexButton.TabIndex = 55;
			this.CreateSubcategoriesNextPageRegexButton.Text = "Create Regex";
			this.CreateSubcategoriesNextPageRegexButton.UseVisualStyleBackColor = true;
			this.CreateSubcategoriesNextPageRegexButton.Click += new System.EventHandler(this.CreateSubcategoriesNextPageRegexButton_Click);
			// 
			// subcategoryNextPageRegexTextBox
			// 
			this.subcategoryNextPageRegexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.subcategoryNextPageRegexTextBox.Location = new System.Drawing.Point(3, 238);
			this.subcategoryNextPageRegexTextBox.Name = "subcategoryNextPageRegexTextBox";
			this.subcategoryNextPageRegexTextBox.Size = new System.Drawing.Size(451, 20);
			this.subcategoryNextPageRegexTextBox.TabIndex = 54;
			// 
			// label28
			// 
			this.label28.AutoSize = true;
			this.label28.Location = new System.Drawing.Point(0, 222);
			this.label28.Name = "label28";
			this.label28.Size = new System.Drawing.Size(86, 13);
			this.label28.TabIndex = 53;
			this.label28.Text = "NextPageRegEx";
			// 
			// manageStaticSubCategoriesButton
			// 
			this.manageStaticSubCategoriesButton.Location = new System.Drawing.Point(3, 264);
			this.manageStaticSubCategoriesButton.Name = "manageStaticSubCategoriesButton";
			this.manageStaticSubCategoriesButton.Size = new System.Drawing.Size(115, 23);
			this.manageStaticSubCategoriesButton.TabIndex = 34;
			this.manageStaticSubCategoriesButton.Text = "Static Subcategories";
			this.manageStaticSubCategoriesButton.UseVisualStyleBackColor = true;
			this.manageStaticSubCategoriesButton.Click += new System.EventHandler(this.manageStaticSubCategoriesButton_Click);
			// 
			// CreateSubcategoriesRegexButton
			// 
			this.CreateSubcategoriesRegexButton.Location = new System.Drawing.Point(3, 3);
			this.CreateSubcategoriesRegexButton.Name = "CreateSubcategoriesRegexButton";
			this.CreateSubcategoriesRegexButton.Size = new System.Drawing.Size(81, 23);
			this.CreateSubcategoriesRegexButton.TabIndex = 33;
			this.CreateSubcategoriesRegexButton.Text = "Create Regex";
			this.CreateSubcategoriesRegexButton.UseVisualStyleBackColor = true;
			this.CreateSubcategoriesRegexButton.Click += new System.EventHandler(this.CreateSubcategoriesRegexButton_Click);
			// 
			// subcategoryUrlFormatTextBox
			// 
			this.subcategoryUrlFormatTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.subcategoryUrlFormatTextBox.Location = new System.Drawing.Point(3, 147);
			this.subcategoryUrlFormatTextBox.Name = "subcategoryUrlFormatTextBox";
			this.subcategoryUrlFormatTextBox.Size = new System.Drawing.Size(451, 20);
			this.subcategoryUrlFormatTextBox.TabIndex = 31;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(0, 131);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(154, 13);
			this.label4.TabIndex = 30;
			this.label4.Text = "DynamicSubCategoryUrlFormat";
			// 
			// GetSubCategoriesButton
			// 
			this.GetSubCategoriesButton.Location = new System.Drawing.Point(89, 3);
			this.GetSubCategoriesButton.Name = "GetSubCategoriesButton";
			this.GetSubCategoriesButton.Size = new System.Drawing.Size(75, 23);
			this.GetSubCategoriesButton.TabIndex = 29;
			this.GetSubCategoriesButton.Text = "GetSubCats";
			this.GetSubCategoriesButton.UseVisualStyleBackColor = true;
			this.GetSubCategoriesButton.Click += new System.EventHandler(this.GetSubCategoriesButton_Click);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(0, 32);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(99, 13);
			this.label5.TabIndex = 28;
			this.label5.Text = "SubCategoryRegex";
			// 
			// subcategoryRegexTextBox
			// 
			this.subcategoryRegexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.subcategoryRegexTextBox.Location = new System.Drawing.Point(3, 48);
			this.subcategoryRegexTextBox.Multiline = true;
			this.subcategoryRegexTextBox.Name = "subcategoryRegexTextBox";
			this.subcategoryRegexTextBox.Size = new System.Drawing.Size(451, 80);
			this.subcategoryRegexTextBox.TabIndex = 27;
			// 
			// videoListTabPage
			// 
			this.videoListTabPage.Controls.Add(this.SearchQueryTextBox);
			this.videoListTabPage.Controls.Add(this.GetSearchResultsButton);
			this.videoListTabPage.Controls.Add(this.searchPostStringTextBox);
			this.videoListTabPage.Controls.Add(this.label35);
			this.videoListTabPage.Controls.Add(this.searchUrlTextBox);
			this.videoListTabPage.Controls.Add(this.label34);
			this.videoListTabPage.Controls.Add(this.label32);
			this.videoListTabPage.Controls.Add(this.nextPageUrlDecodingComboBox);
			this.videoListTabPage.Controls.Add(this.label31);
			this.videoListTabPage.Controls.Add(this.videoListUrlDecodingComboBox);
			this.videoListTabPage.Controls.Add(this.CreateNextPageRegexButton);
			this.videoListTabPage.Controls.Add(this.CreateVideoListRegexButton);
			this.videoListTabPage.Controls.Add(this.nextPageRegExUrlFormatStringTextBox);
			this.videoListTabPage.Controls.Add(this.nextPageRegExTextBox);
			this.videoListTabPage.Controls.Add(this.label10);
			this.videoListTabPage.Controls.Add(this.label9);
			this.videoListTabPage.Controls.Add(this.videoThumbFormatStringTextBox);
			this.videoListTabPage.Controls.Add(this.label8);
			this.videoListTabPage.Controls.Add(this.videoListRegexFormatTextBox);
			this.videoListTabPage.Controls.Add(this.label6);
			this.videoListTabPage.Controls.Add(this.GetVideoListButton);
			this.videoListTabPage.Controls.Add(this.label7);
			this.videoListTabPage.Controls.Add(this.videoListRegexTextBox);
			this.videoListTabPage.Location = new System.Drawing.Point(4, 22);
			this.videoListTabPage.Name = "videoListTabPage";
			this.videoListTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.videoListTabPage.Size = new System.Drawing.Size(460, 555);
			this.videoListTabPage.TabIndex = 2;
			this.videoListTabPage.Text = "VideoList";
			this.videoListTabPage.UseVisualStyleBackColor = true;
			// 
			// GetSearchResultsButton
			// 
			this.GetSearchResultsButton.Location = new System.Drawing.Point(3, 458);
			this.GetSearchResultsButton.Name = "GetSearchResultsButton";
			this.GetSearchResultsButton.Size = new System.Drawing.Size(111, 23);
			this.GetSearchResultsButton.TabIndex = 66;
			this.GetSearchResultsButton.Text = "Get Search Results";
			this.GetSearchResultsButton.UseVisualStyleBackColor = true;
			this.GetSearchResultsButton.Click += new System.EventHandler(this.GetSearchResultsButton_Click);
			// 
			// searchPostStringTextBox
			// 
			this.searchPostStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.searchPostStringTextBox.Location = new System.Drawing.Point(3, 432);
			this.searchPostStringTextBox.Name = "searchPostStringTextBox";
			this.searchPostStringTextBox.Size = new System.Drawing.Size(454, 20);
			this.searchPostStringTextBox.TabIndex = 65;
			this.toolTip1.SetToolTip(this.searchPostStringTextBox, "Format string that should be sent as post data for getting the results of a searc" +
        "h. {0} will be replaced with the query. If this is not set, search will be execu" +
        "ted normal as GET.");
			// 
			// label35
			// 
			this.label35.AutoSize = true;
			this.label35.Location = new System.Drawing.Point(0, 416);
			this.label35.Name = "label35";
			this.label35.Size = new System.Drawing.Size(89, 13);
			this.label35.TabIndex = 64;
			this.label35.Text = "SearchPostString";
			// 
			// searchUrlTextBox
			// 
			this.searchUrlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.searchUrlTextBox.Location = new System.Drawing.Point(3, 391);
			this.searchUrlTextBox.Name = "searchUrlTextBox";
			this.searchUrlTextBox.Size = new System.Drawing.Size(454, 20);
			this.searchUrlTextBox.TabIndex = 63;
			this.toolTip1.SetToolTip(this.searchUrlTextBox, "Format string used as Url for getting the results of a search. {0} will be replac" +
        "ed with the query.");
			// 
			// label34
			// 
			this.label34.AutoSize = true;
			this.label34.Location = new System.Drawing.Point(0, 375);
			this.label34.Name = "label34";
			this.label34.Size = new System.Drawing.Size(54, 13);
			this.label34.TabIndex = 62;
			this.label34.Text = "SearchUrl";
			// 
			// label32
			// 
			this.label32.AutoSize = true;
			this.label32.Location = new System.Drawing.Point(0, 314);
			this.label32.Name = "label32";
			this.label32.Size = new System.Drawing.Size(113, 13);
			this.label32.TabIndex = 61;
			this.label32.Text = "NextPageUrlDecoding";
			// 
			// nextPageUrlDecodingComboBox
			// 
			this.nextPageUrlDecodingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.nextPageUrlDecodingComboBox.FormattingEnabled = true;
			this.nextPageUrlDecodingComboBox.Location = new System.Drawing.Point(119, 311);
			this.nextPageUrlDecodingComboBox.Name = "nextPageUrlDecodingComboBox";
			this.nextPageUrlDecodingComboBox.Size = new System.Drawing.Size(66, 21);
			this.nextPageUrlDecodingComboBox.TabIndex = 60;
			// 
			// label31
			// 
			this.label31.AutoSize = true;
			this.label31.Location = new System.Drawing.Point(0, 137);
			this.label31.Name = "label31";
			this.label31.Size = new System.Drawing.Size(109, 13);
			this.label31.TabIndex = 59;
			this.label31.Text = "VideoListUrlDecoding";
			// 
			// videoListUrlDecodingComboBox
			// 
			this.videoListUrlDecodingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.videoListUrlDecodingComboBox.FormattingEnabled = true;
			this.videoListUrlDecodingComboBox.Location = new System.Drawing.Point(115, 134);
			this.videoListUrlDecodingComboBox.Name = "videoListUrlDecodingComboBox";
			this.videoListUrlDecodingComboBox.Size = new System.Drawing.Size(66, 21);
			this.videoListUrlDecodingComboBox.TabIndex = 58;
			// 
			// CreateNextPageRegexButton
			// 
			this.CreateNextPageRegexButton.Location = new System.Drawing.Point(3, 205);
			this.CreateNextPageRegexButton.Name = "CreateNextPageRegexButton";
			this.CreateNextPageRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateNextPageRegexButton.TabIndex = 51;
			this.CreateNextPageRegexButton.Text = "Create Regex";
			this.CreateNextPageRegexButton.UseVisualStyleBackColor = true;
			this.CreateNextPageRegexButton.Click += new System.EventHandler(this.CreateNextPageRegexButton_Click);
			// 
			// CreateVideoListRegexButton
			// 
			this.CreateVideoListRegexButton.Location = new System.Drawing.Point(3, 3);
			this.CreateVideoListRegexButton.Name = "CreateVideoListRegexButton";
			this.CreateVideoListRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateVideoListRegexButton.TabIndex = 50;
			this.CreateVideoListRegexButton.Text = "Create Regex";
			this.CreateVideoListRegexButton.UseVisualStyleBackColor = true;
			this.CreateVideoListRegexButton.Click += new System.EventHandler(this.CreateVideoListRegexButton_Click);
			// 
			// nextPageRegExUrlFormatStringTextBox
			// 
			this.nextPageRegExUrlFormatStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.nextPageRegExUrlFormatStringTextBox.Location = new System.Drawing.Point(3, 286);
			this.nextPageRegExUrlFormatStringTextBox.Name = "nextPageRegExUrlFormatStringTextBox";
			this.nextPageRegExUrlFormatStringTextBox.Size = new System.Drawing.Size(454, 20);
			this.nextPageRegExUrlFormatStringTextBox.TabIndex = 43;
			// 
			// nextPageRegExTextBox
			// 
			this.nextPageRegExTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.nextPageRegExTextBox.Location = new System.Drawing.Point(3, 247);
			this.nextPageRegExTextBox.Name = "nextPageRegExTextBox";
			this.nextPageRegExTextBox.Size = new System.Drawing.Size(451, 20);
			this.nextPageRegExTextBox.TabIndex = 42;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(0, 270);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(158, 13);
			this.label10.TabIndex = 41;
			this.label10.Text = "NextPageRegExUrlFormatString";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(0, 231);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(86, 13);
			this.label9.TabIndex = 40;
			this.label9.Text = "NextPageRegEx";
			// 
			// videoThumbFormatStringTextBox
			// 
			this.videoThumbFormatStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.videoThumbFormatStringTextBox.Location = new System.Drawing.Point(3, 179);
			this.videoThumbFormatStringTextBox.Name = "videoThumbFormatStringTextBox";
			this.videoThumbFormatStringTextBox.Size = new System.Drawing.Size(451, 20);
			this.videoThumbFormatStringTextBox.TabIndex = 39;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(0, 163);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(126, 13);
			this.label8.TabIndex = 38;
			this.label8.Text = "VideoThumbFormatString";
			// 
			// videoListRegexFormatTextBox
			// 
			this.videoListRegexFormatTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.videoListRegexFormatTextBox.Location = new System.Drawing.Point(3, 102);
			this.videoListRegexFormatTextBox.Name = "videoListRegexFormatTextBox";
			this.videoListRegexFormatTextBox.Size = new System.Drawing.Size(451, 20);
			this.videoListRegexFormatTextBox.TabIndex = 37;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(0, 86);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(114, 13);
			this.label6.TabIndex = 36;
			this.label6.Text = "VideoListRegExFormat";
			// 
			// GetVideoListButton
			// 
			this.GetVideoListButton.Location = new System.Drawing.Point(89, 3);
			this.GetVideoListButton.Name = "GetVideoListButton";
			this.GetVideoListButton.Size = new System.Drawing.Size(75, 23);
			this.GetVideoListButton.TabIndex = 35;
			this.GetVideoListButton.Text = "GetVideoList";
			this.GetVideoListButton.UseVisualStyleBackColor = true;
			this.GetVideoListButton.Click += new System.EventHandler(this.GetVideoListButton_Click);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(0, 34);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(81, 13);
			this.label7.TabIndex = 34;
			this.label7.Text = "VideoListRegex";
			// 
			// videoListRegexTextBox
			// 
			this.videoListRegexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.videoListRegexTextBox.Location = new System.Drawing.Point(3, 50);
			this.videoListRegexTextBox.Multiline = true;
			this.videoListRegexTextBox.Name = "videoListRegexTextBox";
			this.videoListRegexTextBox.Size = new System.Drawing.Size(451, 33);
			this.videoListRegexTextBox.TabIndex = 33;
			// 
			// VideoUrlTabPage
			// 
			this.VideoUrlTabPage.Controls.Add(this.label33);
			this.VideoUrlTabPage.Controls.Add(this.videoUrlDecodingComboBox);
			this.VideoUrlTabPage.Controls.Add(this.fileUrlNameFormatStringTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label12);
			this.VideoUrlTabPage.Controls.Add(this.label11);
			this.VideoUrlTabPage.Controls.Add(this.comboBoxResolving);
			this.VideoUrlTabPage.Controls.Add(this.fileUrlPostStringTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label18);
			this.VideoUrlTabPage.Controls.Add(this.getRedirectedFileUrlCheckBox);
			this.VideoUrlTabPage.Controls.Add(this.ResultUrlComboBox);
			this.VideoUrlTabPage.Controls.Add(this.getFileUrlButton);
			this.VideoUrlTabPage.Controls.Add(this.playListUrlResultTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label15);
			this.VideoUrlTabPage.Controls.Add(this.GetPlayListUrlButton);
			this.VideoUrlTabPage.Controls.Add(this.videoUrlResultTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label22);
			this.VideoUrlTabPage.Controls.Add(this.btnPlay);
			this.VideoUrlTabPage.Controls.Add(this.label21);
			this.VideoUrlTabPage.Controls.Add(this.CreateFileUrlRegexButton);
			this.VideoUrlTabPage.Controls.Add(this.fileUrlFormatStringTextBox);
			this.VideoUrlTabPage.Controls.Add(this.fileUrlRegexTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label19);
			this.VideoUrlTabPage.Controls.Add(this.label20);
			this.VideoUrlTabPage.Controls.Add(this.CreatePlayListRegexButton);
			this.VideoUrlTabPage.Controls.Add(this.playlistUrlFormatStringTextBox);
			this.VideoUrlTabPage.Controls.Add(this.playlistUrlRegexTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label16);
			this.VideoUrlTabPage.Controls.Add(this.label17);
			this.VideoUrlTabPage.Controls.Add(this.CreateVideoUrlRegexButton);
			this.VideoUrlTabPage.Controls.Add(this.videoUrlFormatStringTextBox);
			this.VideoUrlTabPage.Controls.Add(this.label13);
			this.VideoUrlTabPage.Controls.Add(this.GetVideoUrlButton);
			this.VideoUrlTabPage.Controls.Add(this.label14);
			this.VideoUrlTabPage.Controls.Add(this.videoUrlRegExTextBox);
			this.VideoUrlTabPage.Location = new System.Drawing.Point(4, 22);
			this.VideoUrlTabPage.Name = "VideoUrlTabPage";
			this.VideoUrlTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.VideoUrlTabPage.Size = new System.Drawing.Size(460, 555);
			this.VideoUrlTabPage.TabIndex = 3;
			this.VideoUrlTabPage.Text = "VideoUrl";
			this.VideoUrlTabPage.UseVisualStyleBackColor = true;
			// 
			// label33
			// 
			this.label33.AutoSize = true;
			this.label33.Location = new System.Drawing.Point(0, 117);
			this.label33.Name = "label33";
			this.label33.Size = new System.Drawing.Size(93, 13);
			this.label33.TabIndex = 89;
			this.label33.Text = "VideoUrlDecoding";
			// 
			// videoUrlDecodingComboBox
			// 
			this.videoUrlDecodingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.videoUrlDecodingComboBox.FormattingEnabled = true;
			this.videoUrlDecodingComboBox.Location = new System.Drawing.Point(99, 114);
			this.videoUrlDecodingComboBox.Name = "videoUrlDecodingComboBox";
			this.videoUrlDecodingComboBox.Size = new System.Drawing.Size(66, 21);
			this.videoUrlDecodingComboBox.TabIndex = 88;
			// 
			// fileUrlNameFormatStringTextBox
			// 
			this.fileUrlNameFormatStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fileUrlNameFormatStringTextBox.Location = new System.Drawing.Point(3, 480);
			this.fileUrlNameFormatStringTextBox.Name = "fileUrlNameFormatStringTextBox";
			this.fileUrlNameFormatStringTextBox.Size = new System.Drawing.Size(451, 20);
			this.fileUrlNameFormatStringTextBox.TabIndex = 87;
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(0, 464);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(123, 13);
			this.label12.TabIndex = 86;
			this.label12.Text = "FileUrlNameFormatString";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(300, 332);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(77, 13);
			this.label11.TabIndex = 85;
			this.label11.Text = "ResolveHoster";
			// 
			// comboBoxResolving
			// 
			this.comboBoxResolving.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxResolving.FormattingEnabled = true;
			this.comboBoxResolving.Location = new System.Drawing.Point(383, 329);
			this.comboBoxResolving.Name = "comboBoxResolving";
			this.comboBoxResolving.Size = new System.Drawing.Size(58, 21);
			this.comboBoxResolving.TabIndex = 84;
			// 
			// fileUrlPostStringTextBox
			// 
			this.fileUrlPostStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fileUrlPostStringTextBox.Location = new System.Drawing.Point(3, 366);
			this.fileUrlPostStringTextBox.Name = "fileUrlPostStringTextBox";
			this.fileUrlPostStringTextBox.Size = new System.Drawing.Size(451, 20);
			this.fileUrlPostStringTextBox.TabIndex = 82;
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(0, 353);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(84, 13);
			this.label18.TabIndex = 81;
			this.label18.Text = "FileUrlPostString";
			// 
			// getRedirectedFileUrlCheckBox
			// 
			this.getRedirectedFileUrlCheckBox.AutoSize = true;
			this.getRedirectedFileUrlCheckBox.Location = new System.Drawing.Point(170, 331);
			this.getRedirectedFileUrlCheckBox.Name = "getRedirectedFileUrlCheckBox";
			this.getRedirectedFileUrlCheckBox.Size = new System.Drawing.Size(124, 17);
			this.getRedirectedFileUrlCheckBox.TabIndex = 80;
			this.getRedirectedFileUrlCheckBox.Text = "GetRedirectedFileUrl";
			this.getRedirectedFileUrlCheckBox.UseVisualStyleBackColor = true;
			// 
			// ResultUrlComboBox
			// 
			this.ResultUrlComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ResultUrlComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ResultUrlComboBox.FormattingEnabled = true;
			this.ResultUrlComboBox.Location = new System.Drawing.Point(3, 528);
			this.ResultUrlComboBox.Name = "ResultUrlComboBox";
			this.ResultUrlComboBox.Size = new System.Drawing.Size(407, 21);
			this.ResultUrlComboBox.TabIndex = 79;
			// 
			// getFileUrlButton
			// 
			this.getFileUrlButton.Location = new System.Drawing.Point(89, 327);
			this.getFileUrlButton.Name = "getFileUrlButton";
			this.getFileUrlButton.Size = new System.Drawing.Size(75, 23);
			this.getFileUrlButton.TabIndex = 78;
			this.getFileUrlButton.Text = "GetFileUrl";
			this.getFileUrlButton.UseVisualStyleBackColor = true;
			this.getFileUrlButton.Click += new System.EventHandler(this.getFileUrlButton_Click);
			// 
			// playListUrlResultTextBox
			// 
			this.playListUrlResultTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.playListUrlResultTextBox.Location = new System.Drawing.Point(3, 302);
			this.playListUrlResultTextBox.Name = "playListUrlResultTextBox";
			this.playListUrlResultTextBox.ReadOnly = true;
			this.playListUrlResultTextBox.Size = new System.Drawing.Size(451, 20);
			this.playListUrlResultTextBox.TabIndex = 77;
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(0, 286);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(86, 13);
			this.label15.TabIndex = 76;
			this.label15.Text = "PlayListUrlResult";
			// 
			// GetPlayListUrlButton
			// 
			this.GetPlayListUrlButton.Location = new System.Drawing.Point(89, 177);
			this.GetPlayListUrlButton.Name = "GetPlayListUrlButton";
			this.GetPlayListUrlButton.Size = new System.Drawing.Size(85, 23);
			this.GetPlayListUrlButton.TabIndex = 75;
			this.GetPlayListUrlButton.Text = "GetPlayListUrl";
			this.GetPlayListUrlButton.UseVisualStyleBackColor = true;
			this.GetPlayListUrlButton.Click += new System.EventHandler(this.GetPlayListUrlButton_Click);
			// 
			// videoUrlResultTextBox
			// 
			this.videoUrlResultTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.videoUrlResultTextBox.Location = new System.Drawing.Point(3, 151);
			this.videoUrlResultTextBox.Name = "videoUrlResultTextBox";
			this.videoUrlResultTextBox.ReadOnly = true;
			this.videoUrlResultTextBox.Size = new System.Drawing.Size(451, 20);
			this.videoUrlResultTextBox.TabIndex = 74;
			// 
			// label22
			// 
			this.label22.AutoSize = true;
			this.label22.Location = new System.Drawing.Point(0, 135);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(77, 13);
			this.label22.TabIndex = 73;
			this.label22.Text = "VideoUrlResult";
			// 
			// btnPlay
			// 
			this.btnPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnPlay.ContextMenuStrip = this.playButtonContextMenuStrip;
			this.btnPlay.Location = new System.Drawing.Point(416, 527);
			this.btnPlay.Name = "btnPlay";
			this.btnPlay.Size = new System.Drawing.Size(38, 23);
			this.btnPlay.TabIndex = 72;
			this.btnPlay.Text = "Play";
			this.toolTip1.SetToolTip(this.btnPlay, "Right-click for more options");
			this.btnPlay.UseVisualStyleBackColor = true;
			this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
			// 
			// playButtonContextMenuStrip
			// 
			this.playButtonContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.play,
            this.copyUrl,
            this.checkValid});
			this.playButtonContextMenuStrip.Name = "playButtonContextMenuStrip";
			this.playButtonContextMenuStrip.Size = new System.Drawing.Size(150, 70);
			// 
			// play
			// 
			this.play.Name = "play";
			this.play.Size = new System.Drawing.Size(149, 22);
			this.play.Text = "Play";
			this.play.Click += new System.EventHandler(this.btnPlay_Click);
			// 
			// copyUrl
			// 
			this.copyUrl.Name = "copyUrl";
			this.copyUrl.Size = new System.Drawing.Size(149, 22);
			this.copyUrl.Text = "Copy Url";
			this.copyUrl.Click += new System.EventHandler(this.copyUrl_Click);
			// 
			// checkValid
			// 
			this.checkValid.Name = "checkValid";
			this.checkValid.Size = new System.Drawing.Size(149, 22);
			this.checkValid.Text = "Check Validity";
			this.checkValid.Click += new System.EventHandler(this.checkValid_Click);
			// 
			// label21
			// 
			this.label21.AutoSize = true;
			this.label21.Location = new System.Drawing.Point(0, 514);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(50, 13);
			this.label21.TabIndex = 70;
			this.label21.Text = "ResultUrl";
			// 
			// CreateFileUrlRegexButton
			// 
			this.CreateFileUrlRegexButton.Location = new System.Drawing.Point(3, 327);
			this.CreateFileUrlRegexButton.Name = "CreateFileUrlRegexButton";
			this.CreateFileUrlRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateFileUrlRegexButton.TabIndex = 68;
			this.CreateFileUrlRegexButton.Text = "Create Regex";
			this.CreateFileUrlRegexButton.UseVisualStyleBackColor = true;
			this.CreateFileUrlRegexButton.Click += new System.EventHandler(this.CreateFileUrlRegexButton_Click);
			// 
			// fileUrlFormatStringTextBox
			// 
			this.fileUrlFormatStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fileUrlFormatStringTextBox.Location = new System.Drawing.Point(3, 441);
			this.fileUrlFormatStringTextBox.Name = "fileUrlFormatStringTextBox";
			this.fileUrlFormatStringTextBox.Size = new System.Drawing.Size(451, 20);
			this.fileUrlFormatStringTextBox.TabIndex = 67;
			// 
			// fileUrlRegexTextBox
			// 
			this.fileUrlRegexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fileUrlRegexTextBox.Location = new System.Drawing.Point(3, 402);
			this.fileUrlRegexTextBox.Name = "fileUrlRegexTextBox";
			this.fileUrlRegexTextBox.Size = new System.Drawing.Size(451, 20);
			this.fileUrlRegexTextBox.TabIndex = 66;
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point(0, 425);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(95, 13);
			this.label19.TabIndex = 65;
			this.label19.Text = "FileUrlFormatString";
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Location = new System.Drawing.Point(0, 386);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(68, 13);
			this.label20.TabIndex = 64;
			this.label20.Text = "FileUrlRegEx";
			// 
			// CreatePlayListRegexButton
			// 
			this.CreatePlayListRegexButton.Location = new System.Drawing.Point(3, 177);
			this.CreatePlayListRegexButton.Name = "CreatePlayListRegexButton";
			this.CreatePlayListRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreatePlayListRegexButton.TabIndex = 62;
			this.CreatePlayListRegexButton.Text = "Create Regex";
			this.CreatePlayListRegexButton.UseVisualStyleBackColor = true;
			this.CreatePlayListRegexButton.Click += new System.EventHandler(this.CreatePlayListRegexButton_Click);
			// 
			// playlistUrlFormatStringTextBox
			// 
			this.playlistUrlFormatStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.playlistUrlFormatStringTextBox.Location = new System.Drawing.Point(3, 263);
			this.playlistUrlFormatStringTextBox.Name = "playlistUrlFormatStringTextBox";
			this.playlistUrlFormatStringTextBox.Size = new System.Drawing.Size(451, 20);
			this.playlistUrlFormatStringTextBox.TabIndex = 61;
			// 
			// playlistUrlRegexTextBox
			// 
			this.playlistUrlRegexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.playlistUrlRegexTextBox.Location = new System.Drawing.Point(3, 224);
			this.playlistUrlRegexTextBox.Name = "playlistUrlRegexTextBox";
			this.playlistUrlRegexTextBox.Size = new System.Drawing.Size(451, 20);
			this.playlistUrlRegexTextBox.TabIndex = 60;
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(0, 247);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(111, 13);
			this.label16.TabIndex = 59;
			this.label16.Text = "PlaylistUrlFormatString";
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Location = new System.Drawing.Point(0, 208);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(83, 13);
			this.label17.TabIndex = 58;
			this.label17.Text = "PlaylistUrlRegex";
			// 
			// CreateVideoUrlRegexButton
			// 
			this.CreateVideoUrlRegexButton.Location = new System.Drawing.Point(3, 3);
			this.CreateVideoUrlRegexButton.Name = "CreateVideoUrlRegexButton";
			this.CreateVideoUrlRegexButton.Size = new System.Drawing.Size(80, 23);
			this.CreateVideoUrlRegexButton.TabIndex = 56;
			this.CreateVideoUrlRegexButton.Text = "Create Regex";
			this.CreateVideoUrlRegexButton.UseVisualStyleBackColor = true;
			this.CreateVideoUrlRegexButton.Click += new System.EventHandler(this.CreateVideoUrlRegexButton_Click);
			// 
			// videoUrlFormatStringTextBox
			// 
			this.videoUrlFormatStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.videoUrlFormatStringTextBox.Location = new System.Drawing.Point(3, 89);
			this.videoUrlFormatStringTextBox.Name = "videoUrlFormatStringTextBox";
			this.videoUrlFormatStringTextBox.Size = new System.Drawing.Size(451, 20);
			this.videoUrlFormatStringTextBox.TabIndex = 55;
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(0, 73);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(106, 13);
			this.label13.TabIndex = 54;
			this.label13.Text = "VideoUrlFormatString";
			// 
			// GetVideoUrlButton
			// 
			this.GetVideoUrlButton.Location = new System.Drawing.Point(89, 3);
			this.GetVideoUrlButton.Name = "GetVideoUrlButton";
			this.GetVideoUrlButton.Size = new System.Drawing.Size(75, 23);
			this.GetVideoUrlButton.TabIndex = 53;
			this.GetVideoUrlButton.Text = "GetVideoUrl";
			this.GetVideoUrlButton.UseVisualStyleBackColor = true;
			this.GetVideoUrlButton.Click += new System.EventHandler(this.GetVideoUrlButton_Click);
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(0, 33);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(78, 13);
			this.label14.TabIndex = 52;
			this.label14.Text = "VideoUrlRegex";
			// 
			// videoUrlRegExTextBox
			// 
			this.videoUrlRegExTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.videoUrlRegExTextBox.Location = new System.Drawing.Point(3, 49);
			this.videoUrlRegExTextBox.Multiline = true;
			this.videoUrlRegExTextBox.Name = "videoUrlRegExTextBox";
			this.videoUrlRegExTextBox.Size = new System.Drawing.Size(451, 20);
			this.videoUrlRegExTextBox.TabIndex = 51;
			// 
			// categoryInfoListView
			// 
			this.categoryInfoListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.categoryInfoListView.ContextMenuStrip = this.CategoryViewContextMenuStrip;
			this.categoryInfoListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.categoryInfoListView.FullRowSelect = true;
			this.categoryInfoListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.categoryInfoListView.Location = new System.Drawing.Point(0, 0);
			this.categoryInfoListView.MultiSelect = false;
			this.categoryInfoListView.Name = "categoryInfoListView";
			this.categoryInfoListView.Size = new System.Drawing.Size(955, 129);
			this.categoryInfoListView.TabIndex = 25;
			this.categoryInfoListView.UseCompatibleStateImageBehavior = false;
			this.categoryInfoListView.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Value";
			this.columnHeader2.Width = 891;
			// 
			// CategoryViewContextMenuStrip
			// 
			this.CategoryViewContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyValueToolStripMenuItem});
			this.CategoryViewContextMenuStrip.Name = "CategoryViewContextMenuStrip";
			this.CategoryViewContextMenuStrip.Size = new System.Drawing.Size(135, 26);
			// 
			// copyValueToolStripMenuItem
			// 
			this.copyValueToolStripMenuItem.Name = "copyValueToolStripMenuItem";
			this.copyValueToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.copyValueToolStripMenuItem.Text = "Copy Value";
			this.copyValueToolStripMenuItem.Click += new System.EventHandler(this.copyValueToolStripMenuItem_Click);
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView1.HideSelection = false;
			this.treeView1.Location = new System.Drawing.Point(0, 0);
			this.treeView1.Name = "treeView1";
			this.treeView1.Size = new System.Drawing.Size(483, 581);
			this.treeView1.TabIndex = 26;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 27);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.categoryInfoListView);
			this.splitContainer1.Size = new System.Drawing.Size(955, 714);
			this.splitContainer1.SplitterDistance = 581;
			this.splitContainer1.TabIndex = 27;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.tabControl1);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.treeView1);
			this.splitContainer2.Size = new System.Drawing.Size(955, 581);
			this.splitContainer2.SplitterDistance = 468;
			this.splitContainer2.TabIndex = 0;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.comboBoxSites,
            this.toolStripMenuItem1,
            this.copyToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.debugToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.ShowItemToolTips = true;
			this.menuStrip1.Size = new System.Drawing.Size(955, 27);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadSitesToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
			this.fileToolStripMenuItem.Text = "File";
			// 
			// loadSitesToolStripMenuItem
			// 
			this.loadSitesToolStripMenuItem.Name = "loadSitesToolStripMenuItem";
			this.loadSitesToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
			this.loadSitesToolStripMenuItem.Text = "Load Sites ...";
			this.loadSitesToolStripMenuItem.Click += new System.EventHandler(this.loadSitesToolStripMenuItem_Click);
			// 
			// comboBoxSites
			// 
			this.comboBoxSites.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxSites.Name = "comboBoxSites";
			this.comboBoxSites.Size = new System.Drawing.Size(121, 23);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.CheckOnClick = true;
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(45, 23);
			this.toolStripMenuItem1.Text = "Load";
			this.toolStripMenuItem1.ToolTipText = "Load the selected Site XML.";
			this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.AutoToolTip = true;
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(47, 23);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.ToolTipText = "Copy the current Site XML to the clipboard.";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 23);
			this.helpToolStripMenuItem.Text = "Help";
			this.helpToolStripMenuItem.ToolTipText = "Open the WIKI in your browser.";
			this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
			// 
			// debugToolStripMenuItem
			// 
			this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.categoryToolStripMenuItem,
            this.videoToolStripMenuItem});
			this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
			this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 23);
			this.debugToolStripMenuItem.Text = "Debug";
			// 
			// categoryToolStripMenuItem
			// 
			this.categoryToolStripMenuItem.Name = "categoryToolStripMenuItem";
			this.categoryToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
			this.categoryToolStripMenuItem.Text = "Category";
			this.categoryToolStripMenuItem.Click += new System.EventHandler(this.categoryToolStripMenuItem_Click);
			// 
			// videoToolStripMenuItem
			// 
			this.videoToolStripMenuItem.Name = "videoToolStripMenuItem";
			this.videoToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
			this.videoToolStripMenuItem.Text = "Video";
			this.videoToolStripMenuItem.Click += new System.EventHandler(this.videoToolStripMenuItem_Click);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "OnlineVideoSites.xml";
			this.openFileDialog1.Filter = "xml-Files|*.xml";
			// 
			// SearchQueryTextBox
			// 
			this.SearchQueryTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SearchQueryTextBox.Location = new System.Drawing.Point(120, 460);
			this.SearchQueryTextBox.Name = "SearchQueryTextBox";
			this.SearchQueryTextBox.Size = new System.Drawing.Size(338, 20);
			this.SearchQueryTextBox.TabIndex = 67;
			this.toolTip1.SetToolTip(this.SearchQueryTextBox, "Enter your search string here");
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(955, 741);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "Form1";
			this.Text = "Generic Site Creator";
			this.tabControl1.ResumeLayout(false);
			this.generalTabPage.ResumeLayout(false);
			this.generalTabPage.PerformLayout();
			this.categTabPage.ResumeLayout(false);
			this.categTabPage.PerformLayout();
			this.subCatTabPage.ResumeLayout(false);
			this.subCatTabPage.PerformLayout();
			this.videoListTabPage.ResumeLayout(false);
			this.videoListTabPage.PerformLayout();
			this.VideoUrlTabPage.ResumeLayout(false);
			this.VideoUrlTabPage.PerformLayout();
			this.playButtonContextMenuStrip.ResumeLayout(false);
			this.CategoryViewContextMenuStrip.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage categTabPage;
        private System.Windows.Forms.TextBox categoryUrlFormatTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button GetCategoriesButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox categoryRegexTextbox;
        private System.Windows.Forms.TabPage subCatTabPage;
        private System.Windows.Forms.ListView categoryInfoListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.TextBox subcategoryUrlFormatTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button GetSubCategoriesButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox subcategoryRegexTextBox;
        private System.Windows.Forms.TabPage videoListTabPage;
        private System.Windows.Forms.TextBox videoListRegexFormatTextBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button GetVideoListButton;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox videoListRegexTextBox;
        private System.Windows.Forms.TextBox videoThumbFormatStringTextBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox nextPageRegExUrlFormatStringTextBox;
        private System.Windows.Forms.TextBox nextPageRegExTextBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button CreateCategoryRegexButton;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button CreateSubcategoriesRegexButton;
        private System.Windows.Forms.Button CreateVideoListRegexButton;
        private System.Windows.Forms.Button CreateNextPageRegexButton;
        private System.Windows.Forms.TabPage VideoUrlTabPage;
        private System.Windows.Forms.Button CreateVideoUrlRegexButton;
        private System.Windows.Forms.TextBox videoUrlFormatStringTextBox;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button GetVideoUrlButton;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox videoUrlRegExTextBox;
        private System.Windows.Forms.Button CreateFileUrlRegexButton;
        private System.Windows.Forms.TextBox fileUrlFormatStringTextBox;
        private System.Windows.Forms.TextBox fileUrlRegexTextBox;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Button CreatePlayListRegexButton;
        private System.Windows.Forms.TextBox playlistUrlFormatStringTextBox;
        private System.Windows.Forms.TextBox playlistUrlRegexTextBox;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSitesToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ToolStripComboBox comboBoxSites;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.TextBox videoUrlResultTextBox;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Button GetPlayListUrlButton;
        private System.Windows.Forms.TextBox playListUrlResultTextBox;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button getFileUrlButton;
        private System.Windows.Forms.ComboBox ResultUrlComboBox;
        private System.Windows.Forms.CheckBox getRedirectedFileUrlCheckBox;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox fileUrlPostStringTextBox;
        private System.Windows.Forms.ContextMenuStrip playButtonContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem play;
        private System.Windows.Forms.ToolStripMenuItem copyUrl;
        private System.Windows.Forms.ToolStripMenuItem checkValid;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button manageStaticCategoriesButton;
        private System.Windows.Forms.Button manageStaticSubCategoriesButton;
        private System.Windows.Forms.TabPage generalTabPage;
        private System.Windows.Forms.ComboBox playerComboBox;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.ComboBox cbLanguages;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.CheckBox ageCheckBox;
        private System.Windows.Forms.TextBox baseUrlTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBoxResolving;
        private System.Windows.Forms.TextBox fileUrlNameFormatStringTextBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button makeStaticButton;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem categoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem videoToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip CategoryViewContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem copyValueToolStripMenuItem;
        private System.Windows.Forms.Button CreateCategoryNextPageRegexButton;
        private System.Windows.Forms.TextBox categoryNextPageRegexTextBox;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Button CreateSubcategoriesNextPageRegexButton;
        private System.Windows.Forms.TextBox subcategoryNextPageRegexTextBox;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.ComboBox categoryUrlDecodingComboBox;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.ComboBox subCategoryUrlDecodingComboBox;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.ComboBox nextPageUrlDecodingComboBox;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.ComboBox videoListUrlDecodingComboBox;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.ComboBox videoUrlDecodingComboBox;
		private System.Windows.Forms.Label label34;
		private System.Windows.Forms.TextBox searchUrlTextBox;
		private System.Windows.Forms.Label label35;
		private System.Windows.Forms.TextBox searchPostStringTextBox;
		private System.Windows.Forms.Button GetSearchResultsButton;
		private System.Windows.Forms.TextBox SearchQueryTextBox;
    }
}

