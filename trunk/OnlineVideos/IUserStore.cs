using System;
using System.ComponentModel;

namespace OnlineVideos
{
    /// <summary>
    /// This interface defines the methods needed to store and retrieve arbitrary configurationa data 
    /// for fields tagged with the <see cref="CategoryAttribute"/> with category <see cref="UserConfigurable.ONLINEVIDEOS_USERCONFIGURATION_CATEGORY"/>.
    /// </summary>
    public interface IUserStore
    {
        /// <summary>Get the value from the data store for the given key.</summary>
        /// <param name="key">the unique key used to store and retrieve the value</param>
        /// <param name="decrypt">optional parameter telling the store to decrypt the value</param>
        /// <returns>null when the value was not found in the store, otherwise the value</returns>
        string GetValue(string key, bool decrypt = false);
        
        /// <summary>Store the value in the data store for the given key.</summary>
        /// <param name="key">the unique key used to store and retrieve the value</param>
        /// <param name="value">the value to set</param>
        /// <param name="encrypt">optional parameter telling the store to encrypt the value</param>
        void SetValue(string key, string value, bool encrypt = false);
    }
}
