using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class UserResponse
  {
    private UserEntity user;

    public UserEntity User
    {
      get { return user; }
      set { user = value; }
    }

    public UserResponse()
    {
      User = new UserEntity();
    }
  }
}
