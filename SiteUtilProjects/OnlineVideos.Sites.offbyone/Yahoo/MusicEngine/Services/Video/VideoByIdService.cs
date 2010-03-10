using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  /// <summary>
  /// Returns videos similar to the one passed in.
  /// </summary>
  public class VideoByIdService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("item/{0}", this.Id); }
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
        Dictionary<string, string> _param = new Dictionary<string, string>();
        _param.Add("response", "artists,releases,categories,images");
        return _param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion
    public VideoByIdService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      Id = String.Empty;
    }
  }
}


