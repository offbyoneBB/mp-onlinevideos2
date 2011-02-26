using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites.Cornerstone
{
  public class LogProvider
  {
    public void Error(string s)
    {
      
    }

    public void Error(string s, Exception exception)
    {

    }

    public void Error(string s, string message)
    {

    }

    public void Debug(string s)
    {
      
    }

    public void Debug(string s, Exception exception)
    {

    }

    public void Debug(string s, string message)
    {

    }
    
    public void Warn(string s)
    {

    }

    public void Warn(string s, Exception exception)
    {

    }

    public void Warn(string s, string message)
    {

    }

    public void ErrorException(string s, Exception exception)
    {
      Error(s, exception);
    }
  }
}
