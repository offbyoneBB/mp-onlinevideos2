using System;

namespace OnlineVideos
{
    /// <summary>
    /// This Interface defines the methods needed to store and retrieve arbitrary configurational data for fields tagged with the Category attribute 'OnlineVideosUserConfiguration'.
    /// </summary>
    public interface IUserStore
    {
        string GetValue(string key);
        void SetValue(string key, string value);
    }
}
