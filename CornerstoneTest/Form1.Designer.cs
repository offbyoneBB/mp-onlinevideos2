namespace CornerstoneTest
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
        this.button1 = new System.Windows.Forms.Button();
        this.list_categories = new System.Windows.Forms.ListBox();
        this.label1 = new System.Windows.Forms.Label();
        this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
        this.list_videos = new System.Windows.Forms.ListBox();
        this.button2 = new System.Windows.Forms.Button();
        this.label2 = new System.Windows.Forms.Label();
        this.propertyGrid2 = new System.Windows.Forms.PropertyGrid();
        this.button3 = new System.Windows.Forms.Button();
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.SuspendLayout();
        // 
        // button1
        // 
        this.button1.Location = new System.Drawing.Point(285, 22);
        this.button1.Name = "button1";
        this.button1.Size = new System.Drawing.Size(110, 23);
        this.button1.TabIndex = 0;
        this.button1.Text = "Get Categories";
        this.button1.UseVisualStyleBackColor = true;
        this.button1.Click += new System.EventHandler(this.button1_Click);
        // 
        // list_categories
        // 
        this.list_categories.FormattingEnabled = true;
        this.list_categories.Location = new System.Drawing.Point(12, 22);
        this.list_categories.Name = "list_categories";
        this.list_categories.Size = new System.Drawing.Size(267, 147);
        this.list_categories.TabIndex = 1;
        this.list_categories.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(12, 6);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(57, 13);
        this.label1.TabIndex = 2;
        this.label1.Text = "Categories";
        // 
        // propertyGrid1
        // 
        this.propertyGrid1.HelpVisible = false;
        this.propertyGrid1.Location = new System.Drawing.Point(285, 51);
        this.propertyGrid1.Name = "propertyGrid1";
        this.propertyGrid1.Size = new System.Drawing.Size(281, 118);
        this.propertyGrid1.TabIndex = 3;
        this.propertyGrid1.ToolbarVisible = false;
        // 
        // list_videos
        // 
        this.list_videos.FormattingEnabled = true;
        this.list_videos.Location = new System.Drawing.Point(15, 188);
        this.list_videos.Name = "list_videos";
        this.list_videos.Size = new System.Drawing.Size(267, 160);
        this.list_videos.TabIndex = 4;
        this.list_videos.SelectedIndexChanged += new System.EventHandler(this.list_videos_SelectedIndexChanged);
        // 
        // button2
        // 
        this.button2.Location = new System.Drawing.Point(288, 188);
        this.button2.Name = "button2";
        this.button2.Size = new System.Drawing.Size(107, 23);
        this.button2.TabIndex = 5;
        this.button2.Text = "Get videos";
        this.button2.UseVisualStyleBackColor = true;
        this.button2.Click += new System.EventHandler(this.button2_Click);
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(12, 172);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(39, 13);
        this.label2.TabIndex = 6;
        this.label2.Text = "Videos";
        // 
        // propertyGrid2
        // 
        this.propertyGrid2.HelpVisible = false;
        this.propertyGrid2.Location = new System.Drawing.Point(288, 217);
        this.propertyGrid2.Name = "propertyGrid2";
        this.propertyGrid2.Size = new System.Drawing.Size(281, 131);
        this.propertyGrid2.TabIndex = 7;
        this.propertyGrid2.ToolbarVisible = false;
        // 
        // button3
        // 
        this.button3.Location = new System.Drawing.Point(468, 359);
        this.button3.Name = "button3";
        this.button3.Size = new System.Drawing.Size(101, 23);
        this.button3.TabIndex = 8;
        this.button3.Text = "Get video url";
        this.button3.UseVisualStyleBackColor = true;
        this.button3.Click += new System.EventHandler(this.button3_Click);
        // 
        // textBox1
        // 
        this.textBox1.Location = new System.Drawing.Point(15, 359);
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(447, 20);
        this.textBox1.TabIndex = 9;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(585, 414);
        this.Controls.Add(this.textBox1);
        this.Controls.Add(this.button3);
        this.Controls.Add(this.propertyGrid2);
        this.Controls.Add(this.label2);
        this.Controls.Add(this.button2);
        this.Controls.Add(this.list_videos);
        this.Controls.Add(this.propertyGrid1);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.list_categories);
        this.Controls.Add(this.button1);
        this.Name = "Form1";
        this.Text = "Form1";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.ListBox list_categories;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.PropertyGrid propertyGrid1;
    private System.Windows.Forms.ListBox list_videos;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.PropertyGrid propertyGrid2;
    private System.Windows.Forms.Button button3;
    private System.Windows.Forms.TextBox textBox1;
  }
}

