using System;

namespace OnlineVideos
{
    public interface IUserStore
    {
        string GetValue(string key);
        void SetValue(string key, string value);
    }
}
