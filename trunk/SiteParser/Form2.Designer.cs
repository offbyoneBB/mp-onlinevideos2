namespace SiteParser
{
    partial class Form2
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.findTextBox = new System.Windows.Forms.TextBox();
            this.findButton = new System.Windows.Forms.Button();
            this.insertComboBox = new System.Windows.Forms.ComboBox();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textToRegexButton = new System.Windows.Forms.Button();
            this.testButton = new System.Windows.Forms.Button();
            this.insertButton = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.regexRichText = new System.Windows.Forms.RichTextBox();
            this.checkBoxOnlySelected = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.TextViewTab = new System.Windows.Forms.TabPage();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.WebViewTab = new System.Windows.Forms.TabPage();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.helpButton = new System.Windows.Forms.Button();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.TextViewTab.SuspendLayout();
            this.WebViewTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point(748, 469);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(829, 469);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // findTextBox
            // 
            this.findTextBox.Location = new System.Drawing.Point(349, 5);
            this.findTextBox.Name = "findTextBox";
            this.findTextBox.Size = new System.Drawing.Size(175, 20);
            this.findTextBox.TabIndex = 31;
            this.findTextBox.TextChanged += new System.EventHandler(this.findTextBox_TextChanged);
            this.findTextBox.Enter += new System.EventHandler(this.findTextBox_Enter);
            this.findTextBox.Leave += new System.EventHandler(this.findTextBox_Leave);
            // 
            // findButton
            // 
            this.findButton.Location = new System.Drawing.Point(530, 3);
            this.findButton.Name = "findButton";
            this.findButton.Size = new System.Drawing.Size(75, 23);
            this.findButton.TabIndex = 30;
            this.findButton.Text = "find";
            this.findButton.UseVisualStyleBackColor = true;
            this.findButton.Click += new System.EventHandler(this.findButton_Click);
            // 
            // insertComboBox
            // 
            this.insertComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.insertComboBox.FormattingEnabled = true;
            this.insertComboBox.Location = new System.Drawing.Point(12, 12);
            this.insertComboBox.Name = "insertComboBox";
            this.insertComboBox.Size = new System.Drawing.Size(170, 21);
            this.insertComboBox.TabIndex = 34;
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.Location = new System.Drawing.Point(3, 32);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(321, 270);
            this.treeView1.TabIndex = 36;
            // 
            // textToRegexButton
            // 
            this.textToRegexButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textToRegexButton.Image = global::SiteParser.Properties.Resources.curved;
            this.textToRegexButton.Location = new System.Drawing.Point(278, 4);
            this.textToRegexButton.Name = "textToRegexButton";
            this.textToRegexButton.Size = new System.Drawing.Size(46, 27);
            this.textToRegexButton.TabIndex = 38;
            this.toolTip1.SetToolTip(this.textToRegexButton, "Paste from web-data");
            this.textToRegexButton.UseVisualStyleBackColor = true;
            this.textToRegexButton.Click += new System.EventHandler(this.textToRegexButton_Click);
            // 
            // testButton
            // 
            this.testButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.testButton.Image = global::SiteParser.Properties.Resources.Intersect;
            this.testButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.testButton.Location = new System.Drawing.Point(270, 3);
            this.testButton.Name = "testButton";
            this.testButton.Size = new System.Drawing.Size(54, 23);
            this.testButton.TabIndex = 37;
            this.testButton.Text = "Test";
            this.testButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.testButton, "Test the regex with the web-data");
            this.testButton.UseVisualStyleBackColor = true;
            this.testButton.Click += new System.EventHandler(this.testButton_Click);
            // 
            // insertButton
            // 
            this.insertButton.Image = global::SiteParser.Properties.Resources.arrowDn;
            this.insertButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.insertButton.Location = new System.Drawing.Point(3, 4);
            this.insertButton.Name = "insertButton";
            this.insertButton.Size = new System.Drawing.Size(56, 27);
            this.insertButton.TabIndex = 35;
            this.insertButton.Text = "Insert";
            this.insertButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip1.SetToolTip(this.insertButton, "Insert template");
            this.insertButton.UseVisualStyleBackColor = true;
            this.insertButton.Click += new System.EventHandler(this.insertBbutton_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 39);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(892, 424);
            this.splitContainer1.SplitterDistance = 329;
            this.splitContainer1.TabIndex = 40;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.regexRichText);
            this.splitContainer2.Panel1.Controls.Add(this.insertButton);
            this.splitContainer2.Panel1.Controls.Add(this.textToRegexButton);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.checkBoxOnlySelected);
            this.splitContainer2.Panel2.Controls.Add(this.treeView1);
            this.splitContainer2.Panel2.Controls.Add(this.testButton);
            this.splitContainer2.Size = new System.Drawing.Size(329, 424);
            this.splitContainer2.SplitterDistance = 115;
            this.splitContainer2.TabIndex = 0;
            // 
            // regexRichText
            // 
            this.regexRichText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.regexRichText.HideSelection = false;
            this.regexRichText.Location = new System.Drawing.Point(5, 37);
            this.regexRichText.Name = "regexRichText";
            this.regexRichText.Size = new System.Drawing.Size(321, 75);
            this.regexRichText.TabIndex = 39;
            this.regexRichText.Text = "";
            this.regexRichText.SelectionChanged += new System.EventHandler(this.regexRichText_SelectionChanged);
            // 
            // checkBoxOnlySelected
            // 
            this.checkBoxOnlySelected.AutoSize = true;
            this.checkBoxOnlySelected.Location = new System.Drawing.Point(5, 7);
            this.checkBoxOnlySelected.Name = "checkBoxOnlySelected";
            this.checkBoxOnlySelected.Size = new System.Drawing.Size(142, 17);
            this.checkBoxOnlySelected.TabIndex = 40;
            this.checkBoxOnlySelected.Text = "OnlySelected (Real-time)";
            this.checkBoxOnlySelected.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.TextViewTab);
            this.tabControl1.Controls.Add(this.WebViewTab);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.Padding = new System.Drawing.Point(0, 0);
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(559, 421);
            this.tabControl1.TabIndex = 35;
            // 
            // TextViewTab
            // 
            this.TextViewTab.Controls.Add(this.richTextBox1);
            this.TextViewTab.Location = new System.Drawing.Point(4, 22);
            this.TextViewTab.Margin = new System.Windows.Forms.Padding(0);
            this.TextViewTab.Name = "TextViewTab";
            this.TextViewTab.Size = new System.Drawing.Size(551, 395);
            this.TextViewTab.TabIndex = 0;
            this.TextViewTab.Text = "TextView";
            this.TextViewTab.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.DetectUrls = false;
            this.richTextBox1.HideSelection = false;
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(551, 395);
            this.richTextBox1.TabIndex = 35;
            this.richTextBox1.Text = "";
            // 
            // WebViewTab
            // 
            this.WebViewTab.Controls.Add(this.webBrowser1);
            this.WebViewTab.Location = new System.Drawing.Point(4, 22);
            this.WebViewTab.Margin = new System.Windows.Forms.Padding(0);
            this.WebViewTab.Name = "WebViewTab";
            this.WebViewTab.Size = new System.Drawing.Size(551, 395);
            this.WebViewTab.TabIndex = 1;
            this.WebViewTab.Text = "WebView";
            this.WebViewTab.UseVisualStyleBackColor = true;
            // 
            // webBrowser1
            // 
            this.webBrowser1.AllowNavigation = false;
            this.webBrowser1.AllowWebBrowserDrop = false;
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(0, 0);
            this.webBrowser1.Margin = new System.Windows.Forms.Padding(0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.ScriptErrorsSuppressed = true;
            this.webBrowser1.Size = new System.Drawing.Size(551, 395);
            this.webBrowser1.TabIndex = 0;
            // 
            // helpButton
            // 
            this.helpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.helpButton.Location = new System.Drawing.Point(829, 5);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(75, 23);
            this.helpButton.TabIndex = 41;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // Form2
            // 
            this.AcceptButton = this.findButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(916, 504);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.insertComboBox);
            this.Controls.Add(this.findTextBox);
            this.Controls.Add(this.findButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form2";
            this.Text = "Create regex";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.TextViewTab.ResumeLayout(false);
            this.WebViewTab.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox findTextBox;
        private System.Windows.Forms.Button findButton;
        private System.Windows.Forms.ComboBox insertComboBox;
        private System.Windows.Forms.Button insertButton;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button testButton;
        private System.Windows.Forms.Button textToRegexButton;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage TextViewTab;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TabPage WebViewTab;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.RichTextBox regexRichText;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.CheckBox checkBoxOnlySelected;
    }
}