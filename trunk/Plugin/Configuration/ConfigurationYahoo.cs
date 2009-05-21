using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using YahooMusicEngine.OnlineDataProvider;
using YahooMusicEngine.Services;
using YahooMusicEngine.Locale;

namespace OnlineVideos
{
  public partial class ConfigurationYahoo : Form
  {
    YahooSettings _setting = new YahooSettings();
    ServiceProvider provider = new ServiceProvider();

    public ConfigurationYahoo()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      _setting.Bandwith = (int) numericUpDown_bandwith.Value;
      _setting.ShowPlayableOnly = checkBox_playable.Checked;
      _setting.Locale = ((LocaleEntity)comboBox_lang.SelectedItem).ApiHost;
      _setting.Format_Title = textBox_format_title.Text;
      _setting.ItemCount = (int)numericUpDown_itemnum.Value;
      _setting.Token = textBox_token.Text;
      _setting.Save();
      this.Close();
    }

    private void SetupForm_Load(object sender, EventArgs e)
    {
      _setting.Load();
      provider.AppId = "DeUZup_IkY7d17O2DzAMPoyxmc55_hTasA--";
      provider.SharedSecret = "d80b9a5766788713e1fadd73e752c7eb";
      provider.SetLocale(_setting.Locale);
      foreach (LocaleEntity ent in provider.AvaiableLocales)
      {
        comboBox_lang.Items.Add(ent);
        if (_setting.Locale == ent.ApiHost)
        {
          comboBox_lang.SelectedItem = ent;
        }
      }
      numericUpDown_bandwith.Value = _setting.Bandwith;
      numericUpDown_itemnum.Value = _setting.ItemCount;
      checkBox_playable.Checked = _setting.ShowPlayableOnly;
      textBox_format_title.Text = _setting.Format_Title;
      textBox_token.Text = _setting.Token;

    }

    private void button2_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void button4_Click(object sender, EventArgs e)
    {
      UserInformationService userinfo = new UserInformationService();
      provider.Token = textBox_token.Text;
      provider.GetData(userinfo);
      if (string.IsNullOrEmpty(userinfo.Response.User.Ymid))
      {
        MessageBox.Show("Test fail, wrong token");
      }
      else
      {
        MessageBox.Show("Verification done");
      }

    }

    private void button3_Click(object sender, EventArgs e)
    {
      BrowserForm bwr = new BrowserForm();
      Yahoo.Authentication auth = new Yahoo.Authentication(provider.AppId, provider.SharedSecret);
      bwr.webBrowser1.Url = auth.GetUserLogOnAddress();
      bwr.ShowDialog();
      textBox_token.Text = bwr.Token;
      // Redirect the user to the use sign-in page  
    }
  }
}