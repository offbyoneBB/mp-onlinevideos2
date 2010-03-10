using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  /// <summary>
  /// Returns videos for a given category (ie. genre, theme, era).
  /// </summary>
  public class VideosForACategoryService : VideoServiceBase,IService
  {
    #region IService Members

    public  override string Resource
    {
      get { return string.Format("list/category/{0}",this.Category); }
    }

    public override Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        param.Add("count", Count.ToString());
        param.Add("start", Start.ToString());
        return param;
      }
      set
      {
      }
    }

 

    #endregion

  
    public VideosForACategoryService()
    {
      Items = new List<VideoResponse>();
      Start = 1;
      Count = 25;
    }

  }
}
