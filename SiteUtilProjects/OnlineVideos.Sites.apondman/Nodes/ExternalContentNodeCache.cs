using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman
{
    using Interfaces;

    /// <summary>
    /// Caching manager for external content nodes
    /// </summary>
    public class ExternalContentNodeCache
    {
        private Dictionary<Type, Dictionary<string, IExternalContentNode>> cache;

        public ExternalContentNodeCache()
        {
            this.cache = new Dictionary<Type, Dictionary<string, IExternalContentNode>>();
        }

        /// <summary>
        /// Returns an data object of the specified type from the cache
        /// </summary>
        /// <typeparam name="T">object type</typeparam>
        /// <param name="id">tcm id</param>
        /// <returns></returns>
        public T Get<T>(string id) where T : class, IExternalContentNode
        {
            IExternalContentNode obj;

            Dictionary<string, IExternalContentNode> objCache = getDataObjectCache(typeof(T));
            
            if (objCache != null && objCache.TryGetValue(id, out obj))
            {
                return obj as T;
            }

            return null;
        }

        /// <summary>
        /// Returns all data objects of the specified type from the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IList<T> GetAll<T>() where T : class, IExternalContentNode
        {
            Dictionary<string, IExternalContentNode> objCache = getDataObjectCache(typeof(T));
            return objCache.Values.Cast<T>().ToList();
        }

        /// <summary>
        /// Inserts or updates a data object into the cache
        /// </summary>
        /// <param name="obj">object to add</param>
        public void Add(IExternalContentNode obj)
        {
            Dictionary<string, IExternalContentNode> objCache = getDataObjectCache(obj);
            if (objCache != null)
            {
                objCache[obj.Uri] = obj;
            }
        }

        /// <summary>
        /// Inserts or updates a data object collection into the cache
        /// </summary>
        /// <param name="list"></param>
        public void Add(IList<IExternalContentNode> list)
        {
            foreach (IExternalContentNode obj in list)
            {
                Add(obj);
            }
        }

        /// <summary>
        /// Removes a data object from the cache
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(IExternalContentNode obj)
        {
            Dictionary<string, IExternalContentNode> objCache = getDataObjectCache(obj);

            if (objCache != null && objCache.ContainsKey(obj.Uri))
            {
                objCache.Remove(obj.Uri);
            }
        }

        #region Helpers

        /// <summary>
        /// Gets or creates a data cache collection for the specified object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Dictionary<string, IExternalContentNode> getDataObjectCache(IExternalContentNode obj)
        {
            if (obj == null || obj.Uri == null)
            {
                return null;
            }

            Type objType = obj.GetType();
            return getDataObjectCache(objType);
        }

        /// <summary>
        /// Gets or creates a data cache collection for the specified object type
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Dictionary<string, IExternalContentNode> getDataObjectCache(Type objType)
        {
            // todo: thread safety
            Dictionary<string, IExternalContentNode> objCache;
            if (!this.cache.TryGetValue(objType, out objCache))
            {
                objCache = new Dictionary<string, IExternalContentNode>();
                this.cache[objType] = objCache;
            }

            return objCache;
        }

        #endregion

    }
}
