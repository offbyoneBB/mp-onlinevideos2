using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  /// <summary>
  /// 
  /// </summary>
  public class SearchForVideosService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/search/{0}/{1}",SearchMode.ToString(),HttpUtility.UrlEncode(Keyword)); }
    }
    private string spelltoken;

    public string Spelltoken
    {
      get { return spelltoken; }
      set { spelltoken = value; }
    }

    public override Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> _param = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(Spelltoken))
        {
          _param.Add("spelltoken", Spelltoken);
        }
        if (this.Start != 1)
        {
          _param.Add("start", this.Count.ToString());
        }
        if (this.Count != 25)
        {
          _param.Add("count",this.Count.ToString());
        }
        return _param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    private VideoSearchMode searchMode;

    public VideoSearchMode SearchMode
    {
      get { return searchMode; }
      set { searchMode = value; }
    }

    private string keyword;

    /// <summary>
    /// Keyword to search for .
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

    public SearchForVideosService()
    {
      this.SearchMode = VideoSearchMode.video;
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      SpellResults = new List<SpellResultEntity>();
      Spelltoken = string.Empty;
      Count = 25;
      Start = 1;
    }

    public override void Parse(System.Xml.XmlDocument doc)
    {
      SpellResults.Clear();
      base.Parse(doc);
      XmlNodeList spellnodes = doc.SelectNodes("Videos/SpellResults/SpellResult");
      foreach (XmlNode node in spellnodes)
      {
        SpellResults.Add(new SpellResultEntity(node));
      }
    }

  }
}
