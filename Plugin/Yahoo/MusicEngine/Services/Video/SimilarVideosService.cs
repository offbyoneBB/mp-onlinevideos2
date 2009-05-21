using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  /// <summary>
  /// Returns videos similar to the one passed in.
  /// </summary>
  public class SimilarVideosService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/similar/{0}", this.Id); }
    }

    private string id;

    /// <summary>
    /// Video Id for other services
    /// </summary>
    /// <value>The id.</value>
    public string Id
    {
      get { return id; }
      set { id = value; }
    }

    public override Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        //if (this.Count != 25)
        //  param.Add("count", this.Count.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion
    public SimilarVideosService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      Count = 25;
    }
  }
}
