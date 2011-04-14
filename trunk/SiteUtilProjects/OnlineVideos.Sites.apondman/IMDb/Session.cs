using System;

namespace OnlineVideos.Sites.Pondman.IMDb
{
    using OnlineVideos.Sites.Pondman.Interfaces;

    public class Session
    {

        /// <summary>
        /// Returns the current configuration object instance
        /// </summary>
        public Settings Settings
        {
            get {
                return this.settings;
            }
            set
            {
                this.settings = value;
            }
        } protected Settings settings;

        /// <summary>
        /// Delegate for data requests
        /// </summary>
        public Func<string, string> MakeRequest
        {
            get
            {
                return makeRequest;
            }
            set
            {
                makeRequest = value;
            }
        } Func<string, string> makeRequest;

    }
}
