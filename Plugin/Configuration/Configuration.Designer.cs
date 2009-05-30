/*
 * Created by SharpDevelop.
 * User: GZamor1
 * Date: 7/24/2007
 * Time: 9:34 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace OnlineVideos
{
	partial class Configuration
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
            this.label4 = new System.Windows.Forms.Label();
            this.cmbTrailerSize = new System.Windows.Forms.ComboBox();
            this.txtDownloadDir = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.CategoryList = new System.Windows.Forms.ListBox();
            this.btnDeleteRss = new System.Windows.Forms.Button();
            this.txtRssUrl = new System.Windows.Forms.TextBox();
            this.btnRssSave = new System.Windows.Forms.Button();
            this.label26 = new System.Windows.Forms.Label();
            this.txtRssName = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbSiteUtil = new System.Windows.Forms.ComboBox();
            this.tbxSearchUrl = new System.Windows.Forms.TextBox();
            this.lblSearchUrl = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.chkAgeConfirm = new System.Windows.Forms.CheckBox();
            this.label30 = new System.Windows.Forms.Label();
            this.label29 = new System.Windows.Forms.Label();
            this.btnSiteSave = new System.Windows.Forms.Button();
            this.txtSiteName = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.siteList = new System.Windows.Forms.ListBox();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.txtFilters = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtThumbLoc = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label5 = new System.Windows.Forms.Label();
            this.chkUseAgeConfirmation = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnYahooConfig = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageRssLinks = new System.Windows.Forms.TabPage();
            this.tabChannels = new System.Windows.Forms.TabPage();
            this.btnAddChannel = new System.Windows.Forms.Button();
            this.btnAddGroup = new System.Windows.Forms.Button();
            this.btnDeleteChannel = new System.Windows.Forms.Button();
            this.btnSaveChannel = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.tbxStreamUrl = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tbxStreamName = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbxChannelName = new System.Windows.Forms.TextBox();
            this.tvGroups = new System.Windows.Forms.TreeView();
            this.label6 = new System.Windows.Forms.Label();
            this.tbxPin = new System.Windows.Forms.TextBox();
            this.tbxScreenName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnBrowseForDlFolder = new System.Windows.Forms.Button();
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.label15 = new System.Windows.Forms.Label();
            this.cmbYoutubeQuality = new System.Windows.Forms.ComboBox();
            this.tabSites = new System.Windows.Forms.TabPage();
            this.tabPageCodecs = new System.Windows.Forms.TabPage();
            this.groupBoxSplitter = new System.Windows.Forms.GroupBox();
            this.tbxWMVSplitter = new System.Windows.Forms.TextBox();
            this.tbxAVISplitter = new System.Windows.Forms.TextBox();
            this.tbxMP4Splitter = new System.Windows.Forms.TextBox();
            this.tbxFLVSplitter = new System.Windows.Forms.TextBox();
            this.chkWMVSplitterInstalled = new System.Windows.Forms.CheckBox();
            this.chkAVISplitterInstalled = new System.Windows.Forms.CheckBox();
            this.chkMP4SplitterInstalled = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.chkFLVSplitterInstalled = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.cmbDasErsteQuality = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageRssLinks.SuspendLayout();
            this.tabChannels.SuspendLayout();
            this.mainTabControl.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.tabSites.SuspendLayout();
            this.tabPageCodecs.SuspendLayout();
            this.groupBoxSplitter.SuspendLayout();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 135);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(169, 17);
            this.label4.TabIndex = 22;
            this.label4.Text = "AppleTrailers Max Quality";
            // 
            // cmbTrailerSize
            // 
            this.cmbTrailerSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTrailerSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTrailerSize.FormattingEnabled = true;
            this.cmbTrailerSize.Location = new System.Drawing.Point(300, 133);
            this.cmbTrailerSize.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbTrailerSize.Name = "cmbTrailerSize";
            this.cmbTrailerSize.Size = new System.Drawing.Size(249, 24);
            this.cmbTrailerSize.TabIndex = 21;
            // 
            // txtDownloadDir
            // 
            this.txtDownloadDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDownloadDir.Location = new System.Drawing.Point(300, 103);
            this.txtDownloadDir.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtDownloadDir.Name = "txtDownloadDir";
            this.txtDownloadDir.Size = new System.Drawing.Size(209, 22);
            this.txtDownloadDir.TabIndex = 20;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 107);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(118, 17);
            this.label3.TabIndex = 19;
            this.label3.Text = "Download Folder:";
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAdd.Enabled = false;
            this.btnAdd.Location = new System.Drawing.Point(93, 244);
            this.btnAdd.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(69, 28);
            this.btnAdd.TabIndex = 17;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.BtnAddClick);
            // 
            // CategoryList
            // 
            this.CategoryList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.CategoryList.FormattingEnabled = true;
            this.CategoryList.ItemHeight = 16;
            this.CategoryList.Location = new System.Drawing.Point(4, 7);
            this.CategoryList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.CategoryList.Name = "CategoryList";
            this.CategoryList.Size = new System.Drawing.Size(159, 212);
            this.CategoryList.TabIndex = 6;
            this.CategoryList.SelectedIndexChanged += new System.EventHandler(this.CategoryListSelectedIndexChanged);
            // 
            // btnDeleteRss
            // 
            this.btnDeleteRss.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteRss.Enabled = false;
            this.btnDeleteRss.Location = new System.Drawing.Point(4, 244);
            this.btnDeleteRss.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnDeleteRss.Name = "btnDeleteRss";
            this.btnDeleteRss.Size = new System.Drawing.Size(69, 28);
            this.btnDeleteRss.TabIndex = 14;
            this.btnDeleteRss.Text = "Delete";
            this.btnDeleteRss.UseVisualStyleBackColor = true;
            this.btnDeleteRss.Click += new System.EventHandler(this.BtnDeleteRssClick);
            // 
            // txtRssUrl
            // 
            this.txtRssUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRssUrl.Enabled = false;
            this.txtRssUrl.Location = new System.Drawing.Point(296, 48);
            this.txtRssUrl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtRssUrl.Name = "txtRssUrl";
            this.txtRssUrl.Size = new System.Drawing.Size(245, 22);
            this.txtRssUrl.TabIndex = 16;
            // 
            // btnRssSave
            // 
            this.btnRssSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRssSave.Enabled = false;
            this.btnRssSave.Location = new System.Drawing.Point(413, 78);
            this.btnRssSave.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRssSave.Name = "btnRssSave";
            this.btnRssSave.Size = new System.Drawing.Size(127, 28);
            this.btnRssSave.TabIndex = 13;
            this.btnRssSave.Text = "Save RSS Link";
            this.btnRssSave.UseVisualStyleBackColor = true;
            this.btnRssSave.Click += new System.EventHandler(this.BtnRssSaveClick);
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(172, 23);
            this.label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(77, 17);
            this.label26.TabIndex = 9;
            this.label26.Text = "RSS Name";
            // 
            // txtRssName
            // 
            this.txtRssName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRssName.Enabled = false;
            this.txtRssName.Location = new System.Drawing.Point(296, 20);
            this.txtRssName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtRssName.Name = "txtRssName";
            this.txtRssName.Size = new System.Drawing.Size(245, 22);
            this.txtRssName.TabIndex = 15;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(172, 50);
            this.label25.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(68, 17);
            this.label25.TabIndex = 10;
            this.label25.Text = "RSS URL";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.cbSiteUtil);
            this.groupBox1.Controls.Add(this.tbxSearchUrl);
            this.groupBox1.Controls.Add(this.lblSearchUrl);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Controls.Add(this.txtUserId);
            this.groupBox1.Controls.Add(this.chkAgeConfirm);
            this.groupBox1.Controls.Add(this.label30);
            this.groupBox1.Controls.Add(this.label29);
            this.groupBox1.Controls.Add(this.btnSiteSave);
            this.groupBox1.Controls.Add(this.txtSiteName);
            this.groupBox1.Controls.Add(this.label28);
            this.groupBox1.Controls.Add(this.label27);
            this.groupBox1.Controls.Add(this.siteList);
            this.groupBox1.Controls.Add(this.chkEnabled);
            this.groupBox1.Location = new System.Drawing.Point(7, 7);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(555, 263);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sites";
            // 
            // cbSiteUtil
            // 
            this.cbSiteUtil.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSiteUtil.FormattingEnabled = true;
            this.cbSiteUtil.Location = new System.Drawing.Point(300, 82);
            this.cbSiteUtil.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbSiteUtil.Name = "cbSiteUtil";
            this.cbSiteUtil.Size = new System.Drawing.Size(247, 24);
            this.cbSiteUtil.TabIndex = 21;
            // 
            // tbxSearchUrl
            // 
            this.tbxSearchUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxSearchUrl.Location = new System.Drawing.Point(300, 180);
            this.tbxSearchUrl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbxSearchUrl.Name = "tbxSearchUrl";
            this.tbxSearchUrl.Size = new System.Drawing.Size(247, 22);
            this.tbxSearchUrl.TabIndex = 20;
            // 
            // lblSearchUrl
            // 
            this.lblSearchUrl.AutoSize = true;
            this.lblSearchUrl.Location = new System.Drawing.Point(176, 183);
            this.lblSearchUrl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSearchUrl.Name = "lblSearchUrl";
            this.lblSearchUrl.Size = new System.Drawing.Size(75, 17);
            this.lblSearchUrl.TabIndex = 19;
            this.lblSearchUrl.Text = "Search Url";
            // 
            // txtPassword
            // 
            this.txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPassword.Enabled = false;
            this.txtPassword.Location = new System.Drawing.Point(300, 148);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(247, 22);
            this.txtPassword.TabIndex = 18;
            // 
            // txtUserId
            // 
            this.txtUserId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUserId.Location = new System.Drawing.Point(300, 117);
            this.txtUserId.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(247, 22);
            this.txtUserId.TabIndex = 17;
            // 
            // chkAgeConfirm
            // 
            this.chkAgeConfirm.Location = new System.Drawing.Point(176, 223);
            this.chkAgeConfirm.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.chkAgeConfirm.Name = "chkAgeConfirm";
            this.chkAgeConfirm.Size = new System.Drawing.Size(139, 30);
            this.chkAgeConfirm.TabIndex = 16;
            this.chkAgeConfirm.Text = "Confirm Age";
            this.chkAgeConfirm.UseVisualStyleBackColor = true;
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(176, 151);
            this.label30.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(69, 17);
            this.label30.TabIndex = 15;
            this.label30.Text = "Password";
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(176, 119);
            this.label29.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(49, 17);
            this.label29.TabIndex = 14;
            this.label29.Text = "UserId";
            // 
            // btnSiteSave
            // 
            this.btnSiteSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSiteSave.Enabled = false;
            this.btnSiteSave.Location = new System.Drawing.Point(447, 224);
            this.btnSiteSave.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSiteSave.Name = "btnSiteSave";
            this.btnSiteSave.Size = new System.Drawing.Size(100, 28);
            this.btnSiteSave.TabIndex = 13;
            this.btnSiteSave.Text = "Save Site";
            this.btnSiteSave.UseVisualStyleBackColor = true;
            this.btnSiteSave.Click += new System.EventHandler(this.BtnSiteSaveClick);
            // 
            // txtSiteName
            // 
            this.txtSiteName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSiteName.Enabled = false;
            this.txtSiteName.Location = new System.Drawing.Point(300, 53);
            this.txtSiteName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtSiteName.Name = "txtSiteName";
            this.txtSiteName.Size = new System.Drawing.Size(247, 22);
            this.txtSiteName.TabIndex = 11;
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(176, 85);
            this.label28.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(60, 17);
            this.label28.TabIndex = 10;
            this.label28.Text = "Site Util:";
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(176, 57);
            this.label27.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(77, 17);
            this.label27.TabIndex = 9;
            this.label27.Text = "Site Name:";
            // 
            // siteList
            // 
            this.siteList.FormattingEnabled = true;
            this.siteList.ItemHeight = 16;
            this.siteList.Location = new System.Drawing.Point(8, 23);
            this.siteList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.siteList.Name = "siteList";
            this.siteList.Size = new System.Drawing.Size(159, 228);
            this.siteList.TabIndex = 5;
            this.siteList.SelectedIndexChanged += new System.EventHandler(this.SiteListSelectedIndexChanged);
            // 
            // chkEnabled
            // 
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Location = new System.Drawing.Point(176, 23);
            this.chkEnabled.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Size = new System.Drawing.Size(82, 21);
            this.chkEnabled.TabIndex = 8;
            this.chkEnabled.Text = "Enabled";
            this.chkEnabled.UseVisualStyleBackColor = true;
            // 
            // txtFilters
            // 
            this.txtFilters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilters.Location = new System.Drawing.Point(300, 71);
            this.txtFilters.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtFilters.Name = "txtFilters";
            this.txtFilters.Size = new System.Drawing.Size(249, 22);
            this.txtFilters.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 78);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(206, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Filter out videos with these tags";
            // 
            // txtThumbLoc
            // 
            this.txtThumbLoc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtThumbLoc.Location = new System.Drawing.Point(300, 39);
            this.txtThumbLoc.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtThumbLoc.Name = "txtThumbLoc";
            this.txtThumbLoc.Size = new System.Drawing.Size(249, 22);
            this.txtThumbLoc.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 43);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Thumbnail Location:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 228);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(145, 17);
            this.label5.TabIndex = 23;
            this.label5.Text = "Use Age Confirmation";
            // 
            // chkUseAgeConfirmation
            // 
            this.chkUseAgeConfirmation.AutoSize = true;
            this.chkUseAgeConfirmation.Location = new System.Drawing.Point(300, 228);
            this.chkUseAgeConfirmation.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.chkUseAgeConfirmation.Name = "chkUseAgeConfirmation";
            this.chkUseAgeConfirmation.Size = new System.Drawing.Size(18, 17);
            this.chkUseAgeConfirmation.TabIndex = 24;
            this.chkUseAgeConfirmation.UseVisualStyleBackColor = true;
            this.chkUseAgeConfirmation.CheckedChanged += new System.EventHandler(this.chkUseAgeConfirmation_CheckedChanged);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(450, 302);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 28);
            this.btnSave.TabIndex = 25;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnYahooConfig
            // 
            this.btnYahooConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnYahooConfig.Location = new System.Drawing.Point(382, 267);
            this.btnYahooConfig.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnYahooConfig.Name = "btnYahooConfig";
            this.btnYahooConfig.Size = new System.Drawing.Size(167, 28);
            this.btnYahooConfig.TabIndex = 26;
            this.btnYahooConfig.Text = "Yahoo Configuration";
            this.btnYahooConfig.UseVisualStyleBackColor = true;
            this.btnYahooConfig.Click += new System.EventHandler(this.btnYahooConfig_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageRssLinks);
            this.tabControl1.Controls.Add(this.tabChannels);
            this.tabControl1.Location = new System.Drawing.Point(7, 277);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(555, 313);
            this.tabControl1.TabIndex = 27;
            // 
            // tabPageRssLinks
            // 
            this.tabPageRssLinks.Controls.Add(this.btnAdd);
            this.tabPageRssLinks.Controls.Add(this.CategoryList);
            this.tabPageRssLinks.Controls.Add(this.btnDeleteRss);
            this.tabPageRssLinks.Controls.Add(this.txtRssUrl);
            this.tabPageRssLinks.Controls.Add(this.btnRssSave);
            this.tabPageRssLinks.Controls.Add(this.label26);
            this.tabPageRssLinks.Controls.Add(this.txtRssName);
            this.tabPageRssLinks.Controls.Add(this.label25);
            this.tabPageRssLinks.Location = new System.Drawing.Point(4, 25);
            this.tabPageRssLinks.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageRssLinks.Name = "tabPageRssLinks";
            this.tabPageRssLinks.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageRssLinks.Size = new System.Drawing.Size(547, 284);
            this.tabPageRssLinks.TabIndex = 0;
            this.tabPageRssLinks.Text = "RssLinks";
            this.tabPageRssLinks.UseVisualStyleBackColor = true;
            // 
            // tabChannels
            // 
            this.tabChannels.Controls.Add(this.btnAddChannel);
            this.tabChannels.Controls.Add(this.btnAddGroup);
            this.tabChannels.Controls.Add(this.btnDeleteChannel);
            this.tabChannels.Controls.Add(this.btnSaveChannel);
            this.tabChannels.Controls.Add(this.label10);
            this.tabChannels.Controls.Add(this.tbxStreamUrl);
            this.tabChannels.Controls.Add(this.label9);
            this.tabChannels.Controls.Add(this.tbxStreamName);
            this.tabChannels.Controls.Add(this.label8);
            this.tabChannels.Controls.Add(this.tbxChannelName);
            this.tabChannels.Controls.Add(this.tvGroups);
            this.tabChannels.Location = new System.Drawing.Point(4, 25);
            this.tabChannels.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabChannels.Name = "tabChannels";
            this.tabChannels.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabChannels.Size = new System.Drawing.Size(547, 284);
            this.tabChannels.TabIndex = 1;
            this.tabChannels.Text = "Channels";
            this.tabChannels.UseVisualStyleBackColor = true;
            // 
            // btnAddChannel
            // 
            this.btnAddChannel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddChannel.AutoSize = true;
            this.btnAddChannel.Enabled = false;
            this.btnAddChannel.Location = new System.Drawing.Point(180, 238);
            this.btnAddChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddChannel.Name = "btnAddChannel";
            this.btnAddChannel.Size = new System.Drawing.Size(123, 33);
            this.btnAddChannel.TabIndex = 25;
            this.btnAddChannel.Text = "Add Stream";
            this.btnAddChannel.UseVisualStyleBackColor = true;
            this.btnAddChannel.Click += new System.EventHandler(this.btnAddChannel_Click);
            // 
            // btnAddGroup
            // 
            this.btnAddGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddGroup.AutoSize = true;
            this.btnAddGroup.Enabled = false;
            this.btnAddGroup.Location = new System.Drawing.Point(85, 238);
            this.btnAddGroup.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddGroup.Name = "btnAddGroup";
            this.btnAddGroup.Size = new System.Drawing.Size(116, 33);
            this.btnAddGroup.TabIndex = 24;
            this.btnAddGroup.Text = "Add Group";
            this.btnAddGroup.UseVisualStyleBackColor = true;
            this.btnAddGroup.Click += new System.EventHandler(this.btnAddGroup_Click);
            // 
            // btnDeleteChannel
            // 
            this.btnDeleteChannel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteChannel.AutoSize = true;
            this.btnDeleteChannel.Enabled = false;
            this.btnDeleteChannel.Location = new System.Drawing.Point(7, 238);
            this.btnDeleteChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnDeleteChannel.Name = "btnDeleteChannel";
            this.btnDeleteChannel.Size = new System.Drawing.Size(79, 33);
            this.btnDeleteChannel.TabIndex = 23;
            this.btnDeleteChannel.Text = "Delete";
            this.btnDeleteChannel.UseVisualStyleBackColor = true;
            this.btnDeleteChannel.Click += new System.EventHandler(this.btnDeleteChannel_Click);
            // 
            // btnSaveChannel
            // 
            this.btnSaveChannel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveChannel.Enabled = false;
            this.btnSaveChannel.Location = new System.Drawing.Point(413, 110);
            this.btnSaveChannel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSaveChannel.Name = "btnSaveChannel";
            this.btnSaveChannel.Size = new System.Drawing.Size(127, 28);
            this.btnSaveChannel.TabIndex = 22;
            this.btnSaveChannel.Text = "Save";
            this.btnSaveChannel.UseVisualStyleBackColor = true;
            this.btnSaveChannel.Click += new System.EventHandler(this.btnSaveChannel_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(211, 82);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(75, 17);
            this.label10.TabIndex = 20;
            this.label10.Text = "Stream Url";
            // 
            // tbxStreamUrl
            // 
            this.tbxStreamUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxStreamUrl.Enabled = false;
            this.tbxStreamUrl.Location = new System.Drawing.Point(325, 80);
            this.tbxStreamUrl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbxStreamUrl.Name = "tbxStreamUrl";
            this.tbxStreamUrl.Size = new System.Drawing.Size(215, 22);
            this.tbxStreamUrl.TabIndex = 21;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(211, 53);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(94, 17);
            this.label9.TabIndex = 18;
            this.label9.Text = "Stream Name";
            // 
            // tbxStreamName
            // 
            this.tbxStreamName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxStreamName.Enabled = false;
            this.tbxStreamName.Location = new System.Drawing.Point(325, 50);
            this.tbxStreamName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbxStreamName.Name = "tbxStreamName";
            this.tbxStreamName.Size = new System.Drawing.Size(215, 22);
            this.tbxStreamName.TabIndex = 19;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(211, 23);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(101, 17);
            this.label8.TabIndex = 16;
            this.label8.Text = "Channel Name";
            // 
            // tbxChannelName
            // 
            this.tbxChannelName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxChannelName.Enabled = false;
            this.tbxChannelName.Location = new System.Drawing.Point(325, 20);
            this.tbxChannelName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbxChannelName.Name = "tbxChannelName";
            this.tbxChannelName.Size = new System.Drawing.Size(215, 22);
            this.tbxChannelName.TabIndex = 17;
            // 
            // tvGroups
            // 
            this.tvGroups.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.tvGroups.Location = new System.Drawing.Point(4, 7);
            this.tvGroups.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tvGroups.Name = "tvGroups";
            this.tvGroups.Size = new System.Drawing.Size(199, 228);
            this.tvGroups.TabIndex = 0;
            this.tvGroups.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvGroups_AfterSelect);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(342, 228);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(28, 17);
            this.label6.TabIndex = 28;
            this.label6.Text = "Pin";
            // 
            // tbxPin
            // 
            this.tbxPin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxPin.Location = new System.Drawing.Point(376, 226);
            this.tbxPin.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbxPin.Name = "tbxPin";
            this.tbxPin.Size = new System.Drawing.Size(173, 22);
            this.tbxPin.TabIndex = 29;
            // 
            // tbxScreenName
            // 
            this.tbxScreenName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxScreenName.Location = new System.Drawing.Point(300, 11);
            this.tbxScreenName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbxScreenName.Name = "tbxScreenName";
            this.tbxScreenName.Size = new System.Drawing.Size(249, 22);
            this.tbxScreenName.TabIndex = 31;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 14);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(173, 17);
            this.label7.TabIndex = 30;
            this.label7.Text = "BasicHome Screen Name:";
            // 
            // btnBrowseForDlFolder
            // 
            this.btnBrowseForDlFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseForDlFolder.AutoSize = true;
            this.btnBrowseForDlFolder.Location = new System.Drawing.Point(516, 101);
            this.btnBrowseForDlFolder.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnBrowseForDlFolder.Name = "btnBrowseForDlFolder";
            this.btnBrowseForDlFolder.Size = new System.Drawing.Size(33, 27);
            this.btnBrowseForDlFolder.TabIndex = 32;
            this.btnBrowseForDlFolder.Text = "...";
            this.btnBrowseForDlFolder.UseVisualStyleBackColor = true;
            this.btnBrowseForDlFolder.Click += new System.EventHandler(this.btnBrowseForDlFolder_Click);
            // 
            // mainTabControl
            // 
            this.mainTabControl.Controls.Add(this.tabGeneral);
            this.mainTabControl.Controls.Add(this.tabSites);
            this.mainTabControl.Controls.Add(this.tabPageCodecs);
            this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabControl.Location = new System.Drawing.Point(0, 0);
            this.mainTabControl.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(577, 625);
            this.mainTabControl.TabIndex = 33;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.cmbDasErsteQuality);
            this.tabGeneral.Controls.Add(this.label16);
            this.tabGeneral.Controls.Add(this.label15);
            this.tabGeneral.Controls.Add(this.cmbYoutubeQuality);
            this.tabGeneral.Controls.Add(this.label1);
            this.tabGeneral.Controls.Add(this.btnBrowseForDlFolder);
            this.tabGeneral.Controls.Add(this.btnYahooConfig);
            this.tabGeneral.Controls.Add(this.txtThumbLoc);
            this.tabGeneral.Controls.Add(this.btnSave);
            this.tabGeneral.Controls.Add(this.tbxScreenName);
            this.tabGeneral.Controls.Add(this.label2);
            this.tabGeneral.Controls.Add(this.label7);
            this.tabGeneral.Controls.Add(this.txtFilters);
            this.tabGeneral.Controls.Add(this.tbxPin);
            this.tabGeneral.Controls.Add(this.label3);
            this.tabGeneral.Controls.Add(this.label6);
            this.tabGeneral.Controls.Add(this.txtDownloadDir);
            this.tabGeneral.Controls.Add(this.cmbTrailerSize);
            this.tabGeneral.Controls.Add(this.label4);
            this.tabGeneral.Controls.Add(this.label5);
            this.tabGeneral.Controls.Add(this.chkUseAgeConfirmation);
            this.tabGeneral.Location = new System.Drawing.Point(4, 25);
            this.tabGeneral.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabGeneral.Size = new System.Drawing.Size(569, 596);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            this.tabGeneral.Click += new System.EventHandler(this.tabGeneral_Click);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(8, 168);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(143, 17);
            this.label15.TabIndex = 34;
            this.label15.Text = "YouTube Max Quality";
            // 
            // cmbYoutubeQuality
            // 
            this.cmbYoutubeQuality.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbYoutubeQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbYoutubeQuality.FormattingEnabled = true;
            this.cmbYoutubeQuality.Items.AddRange(new object[] {
            "Normal",
            "High",
            "HD"});
            this.cmbYoutubeQuality.Location = new System.Drawing.Point(300, 165);
            this.cmbYoutubeQuality.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbYoutubeQuality.Name = "cmbYoutubeQuality";
            this.cmbYoutubeQuality.Size = new System.Drawing.Size(249, 24);
            this.cmbYoutubeQuality.TabIndex = 33;
            // 
            // tabSites
            // 
            this.tabSites.Controls.Add(this.groupBox1);
            this.tabSites.Controls.Add(this.tabControl1);
            this.tabSites.Location = new System.Drawing.Point(4, 25);
            this.tabSites.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabSites.Name = "tabSites";
            this.tabSites.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabSites.Size = new System.Drawing.Size(569, 596);
            this.tabSites.TabIndex = 1;
            this.tabSites.Text = "Sites";
            this.tabSites.UseVisualStyleBackColor = true;
            // 
            // tabPageCodecs
            // 
            this.tabPageCodecs.Controls.Add(this.groupBoxSplitter);
            this.tabPageCodecs.Location = new System.Drawing.Point(4, 25);
            this.tabPageCodecs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageCodecs.Name = "tabPageCodecs";
            this.tabPageCodecs.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageCodecs.Size = new System.Drawing.Size(569, 596);
            this.tabPageCodecs.TabIndex = 2;
            this.tabPageCodecs.Text = "Codecs";
            this.tabPageCodecs.UseVisualStyleBackColor = true;
            // 
            // groupBoxSplitter
            // 
            this.groupBoxSplitter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxSplitter.Controls.Add(this.tbxWMVSplitter);
            this.groupBoxSplitter.Controls.Add(this.tbxAVISplitter);
            this.groupBoxSplitter.Controls.Add(this.tbxMP4Splitter);
            this.groupBoxSplitter.Controls.Add(this.tbxFLVSplitter);
            this.groupBoxSplitter.Controls.Add(this.chkWMVSplitterInstalled);
            this.groupBoxSplitter.Controls.Add(this.chkAVISplitterInstalled);
            this.groupBoxSplitter.Controls.Add(this.chkMP4SplitterInstalled);
            this.groupBoxSplitter.Controls.Add(this.label14);
            this.groupBoxSplitter.Controls.Add(this.label13);
            this.groupBoxSplitter.Controls.Add(this.label12);
            this.groupBoxSplitter.Controls.Add(this.chkFLVSplitterInstalled);
            this.groupBoxSplitter.Controls.Add(this.label11);
            this.groupBoxSplitter.Location = new System.Drawing.Point(8, 6);
            this.groupBoxSplitter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSplitter.Name = "groupBoxSplitter";
            this.groupBoxSplitter.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSplitter.Size = new System.Drawing.Size(553, 134);
            this.groupBoxSplitter.TabIndex = 1;
            this.groupBoxSplitter.TabStop = false;
            this.groupBoxSplitter.Text = "Splitter";
            // 
            // tbxWMVSplitter
            // 
            this.tbxWMVSplitter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxWMVSplitter.Location = new System.Drawing.Point(157, 103);
            this.tbxWMVSplitter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbxWMVSplitter.Name = "tbxWMVSplitter";
            this.tbxWMVSplitter.ReadOnly = true;
            this.tbxWMVSplitter.Size = new System.Drawing.Size(391, 22);
            this.tbxWMVSplitter.TabIndex = 11;
            // 
            // tbxAVISplitter
            // 
            this.tbxAVISplitter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxAVISplitter.Location = new System.Drawing.Point(157, 76);
            this.tbxAVISplitter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbxAVISplitter.Name = "tbxAVISplitter";
            this.tbxAVISplitter.ReadOnly = true;
            this.tbxAVISplitter.Size = new System.Drawing.Size(391, 22);
            this.tbxAVISplitter.TabIndex = 10;
            // 
            // tbxMP4Splitter
            // 
            this.tbxMP4Splitter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxMP4Splitter.Location = new System.Drawing.Point(157, 48);
            this.tbxMP4Splitter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbxMP4Splitter.Name = "tbxMP4Splitter";
            this.tbxMP4Splitter.ReadOnly = true;
            this.tbxMP4Splitter.Size = new System.Drawing.Size(391, 22);
            this.tbxMP4Splitter.TabIndex = 9;
            // 
            // tbxFLVSplitter
            // 
            this.tbxFLVSplitter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxFLVSplitter.Location = new System.Drawing.Point(157, 20);
            this.tbxFLVSplitter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbxFLVSplitter.Name = "tbxFLVSplitter";
            this.tbxFLVSplitter.ReadOnly = true;
            this.tbxFLVSplitter.Size = new System.Drawing.Size(391, 22);
            this.tbxFLVSplitter.TabIndex = 8;
            // 
            // chkWMVSplitterInstalled
            // 
            this.chkWMVSplitterInstalled.AutoSize = true;
            this.chkWMVSplitterInstalled.Enabled = false;
            this.chkWMVSplitterInstalled.Location = new System.Drawing.Point(133, 108);
            this.chkWMVSplitterInstalled.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkWMVSplitterInstalled.Name = "chkWMVSplitterInstalled";
            this.chkWMVSplitterInstalled.Size = new System.Drawing.Size(18, 17);
            this.chkWMVSplitterInstalled.TabIndex = 7;
            this.chkWMVSplitterInstalled.UseVisualStyleBackColor = true;
            // 
            // chkAVISplitterInstalled
            // 
            this.chkAVISplitterInstalled.AutoSize = true;
            this.chkAVISplitterInstalled.Enabled = false;
            this.chkAVISplitterInstalled.Location = new System.Drawing.Point(133, 80);
            this.chkAVISplitterInstalled.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkAVISplitterInstalled.Name = "chkAVISplitterInstalled";
            this.chkAVISplitterInstalled.Size = new System.Drawing.Size(18, 17);
            this.chkAVISplitterInstalled.TabIndex = 6;
            this.chkAVISplitterInstalled.UseVisualStyleBackColor = true;
            // 
            // chkMP4SplitterInstalled
            // 
            this.chkMP4SplitterInstalled.AutoSize = true;
            this.chkMP4SplitterInstalled.Enabled = false;
            this.chkMP4SplitterInstalled.Location = new System.Drawing.Point(133, 52);
            this.chkMP4SplitterInstalled.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkMP4SplitterInstalled.Name = "chkMP4SplitterInstalled";
            this.chkMP4SplitterInstalled.Size = new System.Drawing.Size(18, 17);
            this.chkMP4SplitterInstalled.TabIndex = 5;
            this.chkMP4SplitterInstalled.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(5, 107);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(35, 17);
            this.label14.TabIndex = 4;
            this.label14.Text = "wmv";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(5, 79);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(26, 17);
            this.label13.TabIndex = 3;
            this.label13.Text = "avi";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(5, 50);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(109, 17);
            this.label12.TabIndex = 2;
            this.label12.Text = "mp4 | m4v | mov";
            // 
            // chkFLVSplitterInstalled
            // 
            this.chkFLVSplitterInstalled.AutoSize = true;
            this.chkFLVSplitterInstalled.Enabled = false;
            this.chkFLVSplitterInstalled.Location = new System.Drawing.Point(133, 25);
            this.chkFLVSplitterInstalled.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkFLVSplitterInstalled.Name = "chkFLVSplitterInstalled";
            this.chkFLVSplitterInstalled.Size = new System.Drawing.Size(18, 17);
            this.chkFLVSplitterInstalled.TabIndex = 1;
            this.chkFLVSplitterInstalled.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(5, 23);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(22, 17);
            this.label11.TabIndex = 0;
            this.label11.Text = "flv";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(8, 200);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(118, 17);
            this.label16.TabIndex = 35;
            this.label16.Text = "Das Erste Quality";
            // 
            // cmbDasErsteQuality
            // 
            this.cmbDasErsteQuality.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbDasErsteQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDasErsteQuality.FormattingEnabled = true;
            this.cmbDasErsteQuality.Items.AddRange(new object[] {
            "Low",
            "High"});
            this.cmbDasErsteQuality.Location = new System.Drawing.Point(300, 197);
            this.cmbDasErsteQuality.Name = "cmbDasErsteQuality";
            this.cmbDasErsteQuality.Size = new System.Drawing.Size(249, 24);
            this.cmbDasErsteQuality.TabIndex = 36;
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(577, 625);
            this.Controls.Add(this.mainTabControl);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Configuration";
            this.Text = "Online Videos Configuration";
            this.Load += new System.EventHandler(this.Configuration_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigurationFormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageRssLinks.ResumeLayout(false);
            this.tabPageRssLinks.PerformLayout();
            this.tabChannels.ResumeLayout(false);
            this.tabChannels.PerformLayout();
            this.mainTabControl.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            this.tabSites.ResumeLayout(false);
            this.tabPageCodecs.ResumeLayout(false);
            this.groupBoxSplitter.ResumeLayout(false);
            this.groupBoxSplitter.PerformLayout();
            this.ResumeLayout(false);

		}
		private System.Windows.Forms.ComboBox cmbTrailerSize;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtDownloadDir;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.TextBox txtRssUrl;
		private System.Windows.Forms.TextBox txtRssName;
		private System.Windows.Forms.ListBox CategoryList;
		private System.Windows.Forms.ListBox siteList;
		private System.Windows.Forms.CheckBox chkEnabled;
		private System.Windows.Forms.Label label27;
		private System.Windows.Forms.Label label28;
        private System.Windows.Forms.TextBox txtSiteName;
		private System.Windows.Forms.Button btnSiteSave;
		private System.Windows.Forms.Label label29;
		private System.Windows.Forms.Label label30;
		private System.Windows.Forms.CheckBox chkAgeConfirm;
		private System.Windows.Forms.TextBox txtUserId;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.Button btnRssSave;
        private System.Windows.Forms.Button btnDeleteRss;
		private System.Windows.Forms.TextBox txtFilters;
		private System.Windows.Forms.TextBox txtThumbLoc;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		//private System.Windows.Forms.TabPage General_Tab;
		//private System.Windows.Forms.TabControl tabControl1;
		
		void CheckBox1CheckedChanged(object sender, System.EventArgs e)
		{
			
		}

        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkUseAgeConfirmation;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnYahooConfig;
        private System.Windows.Forms.TextBox tbxSearchUrl;
        private System.Windows.Forms.Label lblSearchUrl;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageRssLinks;
        private System.Windows.Forms.TabPage tabChannels;
        private System.Windows.Forms.TreeView tvGroups;
        private System.Windows.Forms.ComboBox cbSiteUtil;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbxPin;
        private System.Windows.Forms.TextBox tbxScreenName;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbxChannelName;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tbxStreamUrl;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tbxStreamName;
        private System.Windows.Forms.Button btnSaveChannel;
        private System.Windows.Forms.Button btnDeleteChannel;
        private System.Windows.Forms.Button btnAddGroup;
        private System.Windows.Forms.Button btnAddChannel;
        private System.Windows.Forms.Button btnBrowseForDlFolder;
        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabSites;
        private System.Windows.Forms.TabPage tabPageCodecs;
        private System.Windows.Forms.GroupBox groupBoxSplitter;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox chkFLVSplitterInstalled;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox chkWMVSplitterInstalled;
        private System.Windows.Forms.CheckBox chkAVISplitterInstalled;
        private System.Windows.Forms.CheckBox chkMP4SplitterInstalled;
        private System.Windows.Forms.TextBox tbxFLVSplitter;
        private System.Windows.Forms.TextBox tbxWMVSplitter;
        private System.Windows.Forms.TextBox tbxAVISplitter;
        private System.Windows.Forms.TextBox tbxMP4Splitter;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox cmbYoutubeQuality;
        private System.Windows.Forms.ComboBox cmbDasErsteQuality;
        private System.Windows.Forms.Label label16;
	}
}
