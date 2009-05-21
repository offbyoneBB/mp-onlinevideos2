using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class VideosForGivenPublishedListService : VideoServiceBase, IService
  {

    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/published/{0}",this.Id); }
    }

    private string id;

    /// <summary>
    ///	Possible values
    ///      popular (Most Popular Videos)
    ///      new (Recently Added Videos)
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
        if (this.Start != 1)
          param.Add("start", this.Start.ToString());
        if (this.Count != 25)
          param.Add("count", this.Count.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    public VideosForGivenPublishedListService()
      : base()
    {
      Start = 1;
      Id = "new";
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
    }
  }
}
