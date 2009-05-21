namespace OnlineVideos
{
    partial class ConfigurationYahoo
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
        this.label1 = new System.Windows.Forms.Label();
        this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
        this.numericUpDown_bandwith = new System.Windows.Forms.NumericUpDown();
        this.numericUpDown_itemnum = new System.Windows.Forms.NumericUpDown();
        this.button1 = new System.Windows.Forms.Button();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.button4 = new System.Windows.Forms.Button();
        this.button3 = new System.Windows.Forms.Button();
        this.label2 = new System.Windows.Forms.Label();
        this.textBox_token = new System.Windows.Forms.TextBox();
        this.groupBox2 = new System.Windows.Forms.GroupBox();
        this.label9 = new System.Windows.Forms.Label();
        this.checkBox_playable = new System.Windows.Forms.CheckBox();
        this.comboBox_lang = new System.Windows.Forms.ComboBox();
        this.label4 = new System.Windows.Forms.Label();
        this.button2 = new System.Windows.Forms.Button();
        this.tabControl1 = new System.Windows.Forms.TabControl();
        this.tabPage1 = new System.Windows.Forms.TabPage();
        this.tabPage2 = new System.Windows.Forms.TabPage();
        this.groupBox3 = new System.Windows.Forms.GroupBox();
        this.label8 = new System.Windows.Forms.Label();
        this.label7 = new System.Windows.Forms.Label();
        this.label6 = new System.Windows.Forms.Label();
        this.label5 = new System.Windows.Forms.Label();
        this.textBox_format_title = new System.Windows.Forms.TextBox();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_bandwith)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_itemnum)).BeginInit();
        this.groupBox1.SuspendLayout();
        this.groupBox2.SuspendLayout();
        this.tabControl1.SuspendLayout();
        this.tabPage1.SuspendLayout();
        this.tabPage2.SuspendLayout();
        this.groupBox3.SuspendLayout();
        this.SuspendLayout();
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(197, 49);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(51, 13);
        this.label1.TabIndex = 1;
        this.label1.Text = "Bandwith";
        // 
        // numericUpDown_bandwith
        // 
        this.numericUpDown_bandwith.Increment = new decimal(new int[] {
            32,
            0,
            0,
            0});
        this.numericUpDown_bandwith.Location = new System.Drawing.Point(200, 65);
        this.numericUpDown_bandwith.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
        this.numericUpDown_bandwith.Name = "numericUpDown_bandwith";
        this.numericUpDown_bandwith.Size = new System.Drawing.Size(107, 20);
        this.numericUpDown_bandwith.TabIndex = 2;
        this.toolTip1.SetToolTip(this.numericUpDown_bandwith, "For automatic choice 0");
        // 
        // numericUpDown_itemnum
        // 
        this.numericUpDown_itemnum.Location = new System.Drawing.Point(97, 42);
        this.numericUpDown_itemnum.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
        this.numericUpDown_itemnum.Minimum = new decimal(new int[] {
            25,
            0,
            0,
            0});
        this.numericUpDown_itemnum.Name = "numericUpDown_itemnum";
        this.numericUpDown_itemnum.Size = new System.Drawing.Size(53, 20);
        this.numericUpDown_itemnum.TabIndex = 8;
        this.toolTip1.SetToolTip(this.numericUpDown_itemnum, "Number of items to show in lists like popular videos or in search, the speed of r" +
                "esponse depend on number of items\r\n");
        this.numericUpDown_itemnum.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
        // 
        // button1
        // 
        this.button1.Location = new System.Drawing.Point(383, 266);
        this.button1.Name = "button1";
        this.button1.Size = new System.Drawing.Size(75, 23);
        this.button1.TabIndex = 3;
        this.button1.Text = "Ok";
        this.button1.UseVisualStyleBackColor = true;
        this.button1.Click += new System.EventHandler(this.button1_Click);
        // 
        // groupBox1
        // 
        this.groupBox1.Controls.Add(this.button4);
        this.groupBox1.Controls.Add(this.button3);
        this.groupBox1.Controls.Add(this.label2);
        this.groupBox1.Controls.Add(this.textBox_token);
        this.groupBox1.Location = new System.Drawing.Point(6, 98);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(380, 110);
        this.groupBox1.TabIndex = 4;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "Authentication";
        // 
        // button4
        // 
        this.button4.Location = new System.Drawing.Point(225, 62);
        this.button4.Name = "button4";
        this.button4.Size = new System.Drawing.Size(149, 23);
        this.button4.TabIndex = 3;
        this.button4.Text = "Test token";
        this.button4.UseVisualStyleBackColor = true;
        this.button4.Click += new System.EventHandler(this.button4_Click);
        // 
        // button3
        // 
        this.button3.Location = new System.Drawing.Point(6, 62);
        this.button3.Name = "button3";
        this.button3.Size = new System.Drawing.Size(144, 23);
        this.button3.TabIndex = 2;
        this.button3.Text = "Get token";
        this.button3.UseVisualStyleBackColor = true;
        this.button3.Click += new System.EventHandler(this.button3_Click);
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(6, 20);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(38, 13);
        this.label2.TabIndex = 1;
        this.label2.Text = "Token";
        // 
        // textBox_token
        // 
        this.textBox_token.Location = new System.Drawing.Point(6, 36);
        this.textBox_token.Name = "textBox_token";
        this.textBox_token.Size = new System.Drawing.Size(368, 20);
        this.textBox_token.TabIndex = 0;
        // 
        // groupBox2
        // 
        this.groupBox2.Controls.Add(this.label9);
        this.groupBox2.Controls.Add(this.numericUpDown_itemnum);
        this.groupBox2.Controls.Add(this.checkBox_playable);
        this.groupBox2.Location = new System.Drawing.Point(6, 9);
        this.groupBox2.Name = "groupBox2";
        this.groupBox2.Size = new System.Drawing.Size(169, 83);
        this.groupBox2.TabIndex = 5;
        this.groupBox2.TabStop = false;
        this.groupBox2.Text = "Contents";
        // 
        // label9
        // 
        this.label9.AutoSize = true;
        this.label9.Location = new System.Drawing.Point(6, 49);
        this.label9.Name = "label9";
        this.label9.Size = new System.Drawing.Size(72, 13);
        this.label9.TabIndex = 8;
        this.label9.Text = "Items to show";
        // 
        // checkBox_playable
        // 
        this.checkBox_playable.AutoSize = true;
        this.checkBox_playable.Location = new System.Drawing.Point(6, 19);
        this.checkBox_playable.Name = "checkBox_playable";
        this.checkBox_playable.Size = new System.Drawing.Size(144, 17);
        this.checkBox_playable.TabIndex = 6;
        this.checkBox_playable.Text = "Show only playable items";
        this.checkBox_playable.UseVisualStyleBackColor = true;
        // 
        // comboBox_lang
        // 
        this.comboBox_lang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBox_lang.FormattingEnabled = true;
        this.comboBox_lang.Location = new System.Drawing.Point(200, 25);
        this.comboBox_lang.Name = "comboBox_lang";
        this.comboBox_lang.Size = new System.Drawing.Size(186, 21);
        this.comboBox_lang.TabIndex = 6;
        // 
        // label4
        // 
        this.label4.AutoSize = true;
        this.label4.Location = new System.Drawing.Point(197, 9);
        this.label4.Name = "label4";
        this.label4.Size = new System.Drawing.Size(122, 13);
        this.label4.TabIndex = 7;
        this.label4.Text = "Localization for contents";
        // 
        // button2
        // 
        this.button2.Location = new System.Drawing.Point(491, 266);
        this.button2.Name = "button2";
        this.button2.Size = new System.Drawing.Size(75, 23);
        this.button2.TabIndex = 8;
        this.button2.Text = "Cancel";
        this.button2.UseVisualStyleBackColor = true;
        this.button2.Click += new System.EventHandler(this.button2_Click);
        // 
        // tabControl1
        // 
        this.tabControl1.Controls.Add(this.tabPage1);
        this.tabControl1.Controls.Add(this.tabPage2);
        this.tabControl1.Location = new System.Drawing.Point(2, 1);
        this.tabControl1.Name = "tabControl1";
        this.tabControl1.SelectedIndex = 0;
        this.tabControl1.Size = new System.Drawing.Size(575, 259);
        this.tabControl1.TabIndex = 9;
        // 
        // tabPage1
        // 
        this.tabPage1.Controls.Add(this.groupBox1);
        this.tabPage1.Controls.Add(this.label1);
        this.tabPage1.Controls.Add(this.label4);
        this.tabPage1.Controls.Add(this.numericUpDown_bandwith);
        this.tabPage1.Controls.Add(this.comboBox_lang);
        this.tabPage1.Controls.Add(this.groupBox2);
        this.tabPage1.Location = new System.Drawing.Point(4, 22);
        this.tabPage1.Name = "tabPage1";
        this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
        this.tabPage1.Size = new System.Drawing.Size(567, 233);
        this.tabPage1.TabIndex = 0;
        this.tabPage1.Text = "General";
        this.tabPage1.UseVisualStyleBackColor = true;
        // 
        // tabPage2
        // 
        this.tabPage2.Controls.Add(this.groupBox3);
        this.tabPage2.Location = new System.Drawing.Point(4, 22);
        this.tabPage2.Name = "tabPage2";
        this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
        this.tabPage2.Size = new System.Drawing.Size(567, 233);
        this.tabPage2.TabIndex = 1;
        this.tabPage2.Text = "Label formating";
        this.tabPage2.UseVisualStyleBackColor = true;
        // 
        // groupBox3
        // 
        this.groupBox3.Controls.Add(this.label8);
        this.groupBox3.Controls.Add(this.label7);
        this.groupBox3.Controls.Add(this.label6);
        this.groupBox3.Controls.Add(this.label5);
        this.groupBox3.Controls.Add(this.textBox_format_title);
        this.groupBox3.Location = new System.Drawing.Point(3, 6);
        this.groupBox3.Name = "groupBox3";
        this.groupBox3.Size = new System.Drawing.Size(558, 108);
        this.groupBox3.TabIndex = 0;
        this.groupBox3.TabStop = false;
        this.groupBox3.Text = "Video title";
        // 
        // label8
        // 
        this.label8.AutoSize = true;
        this.label8.Location = new System.Drawing.Point(298, 52);
        this.label8.Name = "label8";
        this.label8.Size = new System.Drawing.Size(84, 13);
        this.label8.TabIndex = 4;
        this.label8.Text = "%rating% - rating";
        // 
        // label7
        // 
        this.label7.AutoSize = true;
        this.label7.Location = new System.Drawing.Point(195, 51);
        this.label7.Name = "label7";
        this.label7.Size = new System.Drawing.Size(74, 13);
        this.label7.TabIndex = 3;
        this.label7.Text = "%year% - Year";
        // 
        // label6
        // 
        this.label6.AutoSize = true;
        this.label6.Location = new System.Drawing.Point(97, 51);
        this.label6.Name = "label6";
        this.label6.Size = new System.Drawing.Size(77, 13);
        this.label6.TabIndex = 2;
        this.label6.Text = "%artist% - Artist";
        // 
        // label5
        // 
        this.label5.AutoSize = true;
        this.label5.Location = new System.Drawing.Point(6, 51);
        this.label5.Name = "label5";
        this.label5.Size = new System.Drawing.Size(68, 13);
        this.label5.TabIndex = 1;
        this.label5.Text = "%title% - Title";
        // 
        // textBox_format_title
        // 
        this.textBox_format_title.Location = new System.Drawing.Point(6, 19);
        this.textBox_format_title.Name = "textBox_format_title";
        this.textBox_format_title.Size = new System.Drawing.Size(546, 20);
        this.textBox_format_title.TabIndex = 0;
        // 
        // ConfigurationYahoo
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(578, 298);
        this.Controls.Add(this.tabControl1);
        this.Controls.Add(this.button2);
        this.Controls.Add(this.button1);
        this.Name = "ConfigurationYahoo";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Yahoo Music Videos Configuration";
        this.toolTip1.SetToolTip(this, "For automatic value choice 0");
        this.Load += new System.EventHandler(this.SetupForm_Load);
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_bandwith)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_itemnum)).EndInit();
        this.groupBox1.ResumeLayout(false);
        this.groupBox1.PerformLayout();
        this.groupBox2.ResumeLayout(false);
        this.groupBox2.PerformLayout();
        this.tabControl1.ResumeLayout(false);
        this.tabPage1.ResumeLayout(false);
        this.tabPage1.PerformLayout();
        this.tabPage2.ResumeLayout(false);
        this.groupBox3.ResumeLayout(false);
        this.groupBox3.PerformLayout();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.NumericUpDown numericUpDown_bandwith;
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.CheckBox checkBox_playable;
    private System.Windows.Forms.ComboBox comboBox_lang;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.TabControl tabControl1;
    private System.Windows.Forms.TabPage tabPage1;
    private System.Windows.Forms.TabPage tabPage2;
    private System.Windows.Forms.GroupBox groupBox3;
    private System.Windows.Forms.TextBox textBox_format_title;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.Label label9;
    private System.Windows.Forms.NumericUpDown numericUpDown_itemnum;
    private System.Windows.Forms.Button button4;
    private System.Windows.Forms.Button button3;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox textBox_token;
  }
}