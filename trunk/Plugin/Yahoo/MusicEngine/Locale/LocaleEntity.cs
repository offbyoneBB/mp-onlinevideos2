using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Locale
{
  public class LocaleEntity
  {
    private string  locale;

    public string  Locale
    {
      get { return locale; }
      set { locale = value; }
    }

    private string  apiHost;

    public string  ApiHost
    {
      get { return apiHost; }
      set { apiHost = value; }
    }

    private string  eID;

    public string  EID
    {
      get { return eID; }
      set { eID = value; }
    }

    private string ympsc;

    public string Ympsc
    {
      get { return ympsc; }
      set { ympsc = value; }
    }

    private string playerLang;

    public string PlayerLang
    {
      get { return playerLang; }
      set { playerLang = value; }
    }

    public LocaleEntity(string _locale, string _apihost, string _eid, string _ympsc, string _playerlang)
    {
      this.Locale = _locale;
      this.ApiHost = _apihost;
      this.EID = _eid;
      this.Ympsc = _ympsc;
      this.PlayerLang = _playerlang;
    }

    public override string ToString()
    {
      return Locale + " - " + PlayerLang;
    }
  }
}
