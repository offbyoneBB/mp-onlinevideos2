using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class PlaysequenceService : VideoServiceBase, IService
  {

        #region IService Members

    public override string Resource
    {
      get { return string.Format("list/playsequence/{0}", this.Ymid); }
    }

    private string ymid;

    public string Ymid
    {
      get { return ymid; }
      set { ymid = value; }
    }

    private string videoID;

    /// <summary>
    /// 	Video ID of the "seed" video for Autoplay (usually this is the first video).
    /// </summary>
    /// <value>The ids.</value>
    public string VideoID
    {
      get { return videoID; }
      set { videoID = value; }
    }


    public override Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        param.Add("videoID", VideoID);
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    public PlaysequenceService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      Ymid = string.Empty;
      VideoID = string.Empty;
    }
  }
}
