namespace OnlineVideos.MPUrlSourceFilter
{
    partial class RtspConnectionPreference
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonRtspDown = new System.Windows.Forms.Button();
            this.buttonRtspUp = new System.Windows.Forms.Button();
            this.listBoxRtspConnectionPreference = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // buttonRtspDown
            // 
            this.buttonRtspDown.BackgroundImage = global::OnlineVideos.Properties.Resources.Down;
            this.buttonRtspDown.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonRtspDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRtspDown.Location = new System.Drawing.Point(112, 36);
            this.buttonRtspDown.Name = "buttonRtspDown";
            this.buttonRtspDown.Size = new System.Drawing.Size(20, 20);
            this.buttonRtspDown.TabIndex = 21;
            this.buttonRtspDown.UseVisualStyleBackColor = true;
            this.buttonRtspDown.Click += new System.EventHandler(this.buttonRtspDown_Click);
            // 
            // buttonRtspUp
            // 
            this.buttonRtspUp.BackgroundImage = global::OnlineVideos.Properties.Resources.Up;
            this.buttonRtspUp.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonRtspUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRtspUp.Location = new System.Drawing.Point(112, 0);
            this.buttonRtspUp.Name = "buttonRtspUp";
            this.buttonRtspUp.Size = new System.Drawing.Size(20, 20);
            this.buttonRtspUp.TabIndex = 20;
            this.buttonRtspUp.UseVisualStyleBackColor = true;
            this.buttonRtspUp.Click += new System.EventHandler(this.buttonRtspUp_Click);
            // 
            // listBoxRtspConnectionPreference
            // 
            this.listBoxRtspConnectionPreference.FormattingEnabled = true;
            this.listBoxRtspConnectionPreference.Location = new System.Drawing.Point(0, 0);
            this.listBoxRtspConnectionPreference.Name = "listBoxRtspConnectionPreference";
            this.listBoxRtspConnectionPreference.Size = new System.Drawing.Size(100, 56);
            this.listBoxRtspConnectionPreference.TabIndex = 19;
            // 
            // RtspConnectionPreference
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonRtspDown);
            this.Controls.Add(this.buttonRtspUp);
            this.Controls.Add(this.listBoxRtspConnectionPreference);
            this.Name = "RtspConnectionPreference";
            this.Size = new System.Drawing.Size(132, 56);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonRtspDown;
        private System.Windows.Forms.Button buttonRtspUp;
        public System.Windows.Forms.ListBox listBoxRtspConnectionPreference;
    }
}
