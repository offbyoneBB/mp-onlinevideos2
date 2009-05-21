using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using YahooMusicEngine.Entities;
using YahooMusicEngine.Services;

namespace YahooMusicEngine.Services
{
  public class UserInformationService:IService
  {
    #region IService Members

    public string ServiceName
    {
      get { return "user"; }
    }

    public string Resource
    {
      get { return "item/current"; }
    }

    private int start;
    public int Start
    {
      get { return start; }
      set { start = value; }
    }

    private int count;

    public int Count
    {
      get { return count; }
      set { count = value; }
    }

    private UserResponse response;

    public UserResponse Response
    {
      get { return response; }
      set { response = value; }
    }


    public Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        //param.Add("count", Count.ToString());
        //param.Add("start", Start.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    public void Parse(System.Xml.XmlDocument doc)
    {

      XmlNode usernode = doc.SelectSingleNode("User");
      if (usernode != null)
      {
        Response.User = new UserEntity(usernode);
      }
    }

    #endregion

    public UserInformationService()
    {
      Start = 0;
      Count = 25;
      Response = new UserResponse();
    }

  }
}
