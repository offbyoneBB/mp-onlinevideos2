using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos
{
    public partial class RegisterEmail : Form
    {
        static System.Text.RegularExpressions.Regex emailRegex = new System.Text.RegularExpressions.Regex(@"([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})");

        public RegisterEmail()
        {
            InitializeComponent();
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbxEmail.Text) || !emailRegex.IsMatch(tbxEmail.Text))
            {
                MessageBox.Show("Please enter a valid Email!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                string msg = "";
                bool success = ws.RegisterEmail(tbxEmail.Text, out msg);
                MessageBox.Show(msg, success ? "Success" : "Error", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }        
    }
}
