namespace OnlineVideos.Sites.Pondman.Cache
{
    using OnlineVideos.Sites.Pondman.Interfaces;

    public static class DataObjectExtensions
    {
        /// <summary>
        /// Store this object in the data manager
        /// </summary>
        public static void Commit(this IDataObject self)
        {
            DataObjectManager.Commit(self);
        }

        /// <summary>
        /// Delete this object from the data manager
        /// </summary>
        public static void Delete(this IDataObject self)
        {
            DataObjectManager.Delete(self);
        }

    }
}