using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos
{
  public partial class BrowserForm : Form
  {
    public BrowserForm()
    {
      InitializeComponent();
    }
    private string token;

    public string Token
    {
      get { return token; }
      set { token = value; }
    }

    private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
    {
      string query = e.Url.Query.Substring(1); //remove the ?
      Token = string.Empty;
      if (e.Url.ToString().Contains("extra.hu"))
      {
        string[] pairs = query.Split(new char[] { '&' });
        foreach (string s in pairs)
        {
          string[] pair = s.Split(new char[] { '=' });
          if (pair[0] == "token")
          {
            Token = pair[1];
          }
        }
        this.Close();
      }
    }

    private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
    {
      this.webBrowser1.Cursor = System.Windows.Forms.Cursors.WaitCursor;
    }

    private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
    {
      this.webBrowser1.Cursor = System.Windows.Forms.Cursors.Default;
    }


  }
}