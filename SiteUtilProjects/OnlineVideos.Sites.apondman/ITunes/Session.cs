using System;
using OnlineVideos.Sites.Pondman.Interfaces;

namespace OnlineVideos.Sites.Pondman.ITunes
{
    public class Session : ISession
    {
        protected ExternalContentNodeCache cache = new ExternalContentNodeCache();

        /// <summary>
        /// Returns the current configuration object instance
        /// </summary>
        Configuration ISession.Config
        {
            get {
                if (this.config == null)
                {
                    this.config = new Configuration();
                }

                return this.config;
            }
            set
            {
                this.config = value;
            }
        } protected Configuration config;

        /// <summary>
        /// Delegate for data requests
        /// </summary>
        Func<string, string> ISession.MakeRequest
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

        /// <summary>
        /// Gets a new external content node object instance
        /// </summary>
        /// <typeparam name="T">type of object</typeparam>
        /// <param name="uri">uri of the object</param>
        /// <returns></returns>
        T ISession.Get<T>(string uri)
        {
            T obj = cache.Get<T>(uri);
            if (obj == null)
            {
                obj = new T();
                obj.Session = this;
                obj.Uri = uri;
            }

            return obj;
        }

    }
}
