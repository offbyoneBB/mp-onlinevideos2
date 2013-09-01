using System;

namespace OnlineVideos
{
    /// <summary>
    /// This Interface defines the methods needed to store and retrieve arbitrary configurationa data 
	/// for fields tagged with the <see cref="CategoryAttribute"/> and category "OnlineVideosUserConfiguration".
    /// </summary>
    public interface IUserStore
    {
        string GetValue(string key);
        void SetValue(string key, string value);
    }
}
