using System;
using System.ComponentModel;

namespace OnlineVideos
{
    /// <summary>
    /// This Interface defines the methods needed to store and retrieve arbitrary configurationa data 
    /// for fields tagged with the <see cref="CategoryAttribute"/> with category <see cref="UserConfigurable.ONLINEVIDEOS_USERCONFIGURATION_CATEGORY"/>.
    /// </summary>
    public interface IUserStore
    {
        string GetValue(string key);
        void SetValue(string key, string value);
    }
}
