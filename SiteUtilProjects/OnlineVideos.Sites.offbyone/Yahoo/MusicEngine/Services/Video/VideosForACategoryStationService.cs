using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class VideosForACategoryStationService : VideosForACategoryService
  {
    public override string  Resource
    {
      get { return string.Format("list/station/category/{0}", this.Category); }
    }

  }
}
