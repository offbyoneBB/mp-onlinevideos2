using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites.Cornerstone.CustomTypes
{
  interface IStringSourcedObject {

    // note, implementing classes MUST provide a parameterless constructor.

    void LoadFromString(string createStr);
    string ToString();
  }
}