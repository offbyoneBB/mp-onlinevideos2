using System.Collections.Generic;

namespace OnlineVideos.Sites.Pondman.Cache
{
    using OnlineVideos.Sites.Pondman.Interfaces;
    
    public static class DataObjectManager
    {
        private static DataObjectCache cache;

        static DataObjectManager()
        {
            cache = new DataObjectCache();
        }

        #region Data Access Methods

        /// <summary>
        /// Returns an object from the data collection
        /// </summary>
        /// <typeparam name="T">object type</typeparam>
        /// <param name="id">primary key</param>
        /// <returns></returns>
        public static T Get<T>(string id) where T : class, IDataObject
        {
            T obj = cache.Get<T>(id);
            if (obj != null)
            {
                return obj;
            }

            // todo: get actual data from the storage provider
            // todo: thread safety

            return null;
        }

        /// <summary>
        /// Returns all objects of the specified type in the data collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IList<T> GetAll<T>() where T : class, IDataObject
        {
            IList<T> list = cache.GetAll<T>();
            if (list != null)
            {
                return list;
            }

            // todo: get actual data from the persistence provider
            // todo: thread safety
            // Cache.Add(list)

            return new List<T>();            
        }

        /// <summary>
        /// Commits a changed or new data object to the data object manager
        /// </summary>
        /// <param name="obj"></param>
        public static void Commit(IDataObject obj)
        {
            // todo: store the object with the storage provider
            // todo: thread safety
            cache.Add(obj);
        }

        /// <summary>
        /// Removes a data object from the data object manager
        /// </summary>
        /// <param name="obj"></param>
        public static void Delete(IDataObject obj)
        {
            // todo: remove the object from the storage provider
            // todo: thread safety
            cache.Remove(obj);
        }

        #endregion

    }
}