using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace YahooMusicEngine
{
  public interface IService
  {

    string ServiceName { get; }
    string Resource { get; }

    Dictionary<string, string> Params { get; set;}


    void Parse(XmlDocument doc);
  }
}
