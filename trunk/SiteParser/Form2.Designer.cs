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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.RegexTextbox = new System.Windows.Forms.TextBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.findTextBox = new System.Windows.Forms.TextBox();
            this.findButton = new System.Windows.Forms.Button();
            this.insertComboBox = new System.Windows.Forms.ComboBox();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.button7 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(742, 482);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Ok";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(823, 482);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // RegexTextbox
            // 
            this.RegexTextbox.HideSelection = false;
            this.RegexTextbox.Location = new System.Drawing.Point(12, 72);
            this.RegexTextbox.Multiline = true;
            this.RegexTextbox.Name = "RegexTextbox";
            this.RegexTextbox.Size = new System.Drawing.Size(319, 79);
            this.RegexTextbox.TabIndex = 17;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.DetectUrls = false;
            this.richTextBox1.HideSelection = false;
            this.richTextBox1.Location = new System.Drawing.Point(337, 32);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(565, 444);
            this.richTextBox1.TabIndex = 33;
            this.richTextBox1.Text = "";
            // 
            // findTextBox
            // 
            this.findTextBox.Location = new System.Drawing.Point(414, 4);
            this.findTextBox.Name = "findTextBox";
            this.findTextBox.Size = new System.Drawing.Size(175, 20);
            this.findTextBox.TabIndex = 31;
            this.findTextBox.TextChanged += new System.EventHandler(this.findTextBox_TextChanged);
            // 
            // findButton
            // 
            this.findButton.Location = new System.Drawing.Point(595, 2);
            this.findButton.Name = "findButton";
            this.findButton.Size = new System.Drawing.Size(75, 23);
            this.findButton.TabIndex = 30;
            this.findButton.Text = "find";
            this.findButton.UseVisualStyleBackColor = true;
            this.findButton.Click += new System.EventHandler(this.button4_Click);
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
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.treeView1.Location = new System.Drawing.Point(12, 186);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(319, 288);
            this.treeView1.TabIndex = 36;
            // 
            // button7
            // 
            this.button7.Image = global::SiteParser.Properties.Resources.curved;
            this.button7.Location = new System.Drawing.Point(285, 39);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(46, 27);
            this.button7.TabIndex = 38;
            this.toolTip1.SetToolTip(this.button7, "Paste from web-data");
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button6
            // 
            this.button6.Image = global::SiteParser.Properties.Resources.Intersect;
            this.button6.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.button6.Location = new System.Drawing.Point(277, 157);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(54, 23);
            this.button6.TabIndex = 37;
            this.button6.Text = "Test";
            this.button6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.button6, "Test the regex with the web-data");
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button5
            // 
            this.button5.Image = global::SiteParser.Properties.Resources.arrowDn;
            this.button5.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button5.Location = new System.Drawing.Point(12, 39);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(56, 27);
            this.button5.TabIndex = 35;
            this.button5.Text = "Insert";
            this.button5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip1.SetToolTip(this.button5, "Insert template");
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(334, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 39;
            this.label1.Text = "Web-data";
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(910, 517);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.insertComboBox);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.findTextBox);
            this.Controls.Add(this.findButton);
            this.Controls.Add(this.RegexTextbox);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form2";
            this.Text = "Create regex";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox RegexTextbox;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox findTextBox;
        private System.Windows.Forms.Button findButton;
        private System.Windows.Forms.ComboBox insertComboBox;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label1;
    }
}