using System;
using System.Text;
using System.Collections.Generic;
using System.Web;

namespace YahooMusicEngine
{
  /// <summary>
  /// Provide a url for a specified video id
  /// </summary>
  public class VideoPlayer
  {

    private List<string> ids;

    public List<string> VideoIds
    {
      get { return ids; }
      set { ids = value; }
    }

    private string id;

    public string Id
    {
      get { return id; }
      set
      {
        ids.Clear();
        id = value;
      }
    }

    private string eID;
    /// <summary>
    /// Event id used to target a specific music catalog. See Supported Locales  for possible values.
    /// </summary>
    /// <value>The EID.</value>
    public string EID
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

    private bool autoStart;

    public bool AutoStart
    {
      get { return autoStart; }
      set { autoStart = value; }
    }

    private bool controlsEnable;

    public bool ControlsEnable
    {
      get { return controlsEnable; }
      set { controlsEnable = value; }
    }

    private int bandwidth;

    public int Bandwidth
    {
      get { return bandwidth; }
      set { bandwidth = value; }
    }


    public string VideoPlayerUrl
    {

      get
      {
        StringBuilder data = new StringBuilder();
        if (VideoIds.Count > 0)
        {
          string idlist = string.Empty;
          foreach (string s in VideoIds)
          {
            idlist += "v" + s + ",";
          }
          data.Append("id=" + HttpUtility.UrlEncode(idlist.Remove(idlist.Length - 1)));
        }
        else
        {
          data.Append("id=v" + HttpUtility.UrlEncode(Id));
        }
        data.Append("&eID=" + HttpUtility.UrlEncode(EID));
        data.Append("&ympsc=" + HttpUtility.UrlEncode(Ympsc));
        if (AutoStart)
        {
          data.Append("&autoStart=1");
        }
        if (!ControlsEnable)
        {
          data.Append("&controlsEnable=0");
        }
        if (Bandwidth > 0)
        {
          data.Append("&bw=" + HttpUtility.UrlEncode(Bandwidth.ToString()));
        }
        data.Append("&closeEnable=0");
        data.Append("&enableFullScreen=0");
        data.Append("&infoEnable=0");
        data.Append("&nowplayingEnable=0");
        data.Append("&postpanelEnable=0");
        data.Append("&prepanelEnable=0");
        data.Append("&shareEnable=0");
        return string.Format("http://d.yimg.com/cosmos.bcst.yahoo.com/up/fop/embedflv/swf/fop.swf?{0}", data.ToString());
      }
    }


   
    public VideoPlayer(string videoId)
    {
      ids = new List<string>();
      EID = "1301797";
      Ympsc = "4195351";
      Id = videoId;
      AutoStart = false;
      ControlsEnable = true;
      bandwidth = 0;
    }

   }
}
