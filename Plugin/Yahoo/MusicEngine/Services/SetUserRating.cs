using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class SetUserRating : IService
  {
            #region IService Members

    public string ServiceName
    {
      get { return "rating"; }
    }

    public string Resource
    {
      get { return string.Format("item/{0}/video", this.Ymid); }
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
    public string Id
    {
      get { return videoID; }
      set { videoID = value; }
    }

    private int rating;

    public int Rating
    {
      get { return rating; }
      set { rating = value; }
    }
	

    public Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        param.Add("id", Id);
        param.Add("value", Rating.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    public SetUserRating()
    {
      Ymid = string.Empty;
      Id = string.Empty;
      Rating = -1;
    }

    #region IService Members


    public void Parse(System.Xml.XmlDocument doc)
    {
      //throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
