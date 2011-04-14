using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites.Pondman.Cache
{
    using OnlineVideos.Sites.Pondman.Interfaces;
    
    /// <summary>
    /// Business Objects Cache Management
    /// </summary>
    public class DataObjectCache
    {
        private Dictionary<Type, Dictionary<string, IDataObject>> cache;

        public DataObjectCache()
        {
            this.cache = new Dictionary<Type, Dictionary<string, IDataObject>>();
        }

        /// <summary>
        /// Returns an data object of the specified type from the cache
        /// </summary>
        /// <typeparam name="T">object type</typeparam>
        /// <param name="id">tcm id</param>
        /// <returns></returns>
        public T Get<T>(string id) where T : class, IDataObject
        {
            IDataObject obj;

            Dictionary<string, IDataObject> objCache = getDataObjectCache(typeof(T));
            
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
        public IList<T> GetAll<T>() where T : class, IDataObject
        {
            Dictionary<string, IDataObject> objCache = getDataObjectCache(typeof(T));
            return objCache.Values.Cast<T>().ToList();
        }

        /// <summary>
        /// Inserts or updates a data object into the cache
        /// </summary>
        /// <param name="obj">object to add</param>
        public void Add(IDataObject obj)
        {
            Dictionary<string, IDataObject> objCache = getDataObjectCache(obj);
            if (objCache != null)
            {
                objCache[obj.PrimaryKey] = obj;
            }
        }

        /// <summary>
        /// Inserts or updates a data object collection into the cache
        /// </summary>
        /// <param name="list"></param>
        public void Add(IList<IDataObject> list)
        {
            foreach (IDataObject obj in list)
            {
                Add(obj);
            }
        }

        /// <summary>
        /// Removes a data object from the cache
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(IDataObject obj)
        {
            Dictionary<string, IDataObject> objCache = getDataObjectCache(obj);

            if (objCache != null && objCache.ContainsKey(obj.PrimaryKey))
            {
                objCache.Remove(obj.PrimaryKey);
            }
        }

        #region Helpers

        /// <summary>
        /// Gets or creates a data cache collection for the specified object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Dictionary<string, IDataObject> getDataObjectCache(IDataObject obj)
        {
            // todo: thread safety
            if (obj == null || obj.PrimaryKey == null)
                return null;

            Type objType = obj.GetType();
            return getDataObjectCache(objType);
        }

        /// <summary>
        /// Gets or creates a data cache collection for the specified object type
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Dictionary<string, IDataObject> getDataObjectCache(Type objType)
        {
            // todo: thread safety
            Dictionary<string, IDataObject> objCache;
            if (!this.cache.TryGetValue(objType, out objCache))
            {
                objCache = new Dictionary<string, IDataObject>();
                this.cache[objType] = objCache;
            }

            return objCache;
        }

        #endregion

    }
}