using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  /// <summary>
  /// Returns videos for artists whose sort name start with the given letter. '0' indicates all non-alpha characters.
  /// </summary>
  public class VideosForAGivenArtistLetterService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/artist/alpha/{0}", this.Letter.Substring(0,1)); }
    }

    private string letter;

    public string Letter
    {
      get { return letter; }
      set { letter = value; }
    }

    public override Dictionary<string, string> Params
    {
      get
      {
        return new Dictionary<string, string>();
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    public VideosForAGivenArtistLetterService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      Letter = " ";
    }
  }
}
