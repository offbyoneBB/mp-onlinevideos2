using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  /// <summary>
  /// Returns artists matching a search keyword on a given field
  /// </summary>
  public class SearchForArtistsService : ArtistServiceBase, IService
  {

    #region IService Members

    public string ServiceName
    {
      get { return "artist"; }
    }

    public string Resource
    {
      get { return string.Format("list/search/artist/{0}", HttpUtility.UrlEncode(Keyword)); }
    }

    public override Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(Response))
          param.Add("response", this.Response);
        if(!string.IsNullOrEmpty(Spelltoken))
          param.Add("spelltoken", this.Spelltoken);
        if (this.Count != 25)
          param.Add("count", this.Count.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    private string keyword;

    /// <summary>
    /// 	Keyword to search for
    /// </summary>
    /// <value>The keyword.</value>
    public string Keyword
    {
      get { return keyword; }
      set { keyword = value; }
    }

    private List<SpellResultEntity> spellResults;

    public List<SpellResultEntity> SpellResults
    {
      get { return spellResults; }
      set { spellResults = value; }
    }

    private string spelltoken;

    public string Spelltoken
    {
      get { return spelltoken; }
      set { spelltoken = value; }
    }

    public SearchForArtistsService()
    {
      Items = new List<ArtistResponse>();
      SpellResults = new List<SpellResultEntity>();
      Response = string.Empty;
      Spelltoken = string.Empty;
      Count = 25;
    }

    public override void Parse(System.Xml.XmlDocument doc)
    {
      SpellResults.Clear();
      base.Parse(doc);
      XmlNodeList spellnodes = doc.SelectNodes("Artists/SpellResults/SpellResult");
      foreach (XmlNode node in spellnodes)
      {
        SpellResults.Add(new SpellResultEntity(node));
      }
    }
    #endregion
  }
}
