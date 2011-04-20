namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using OnlineVideos.Sites.Pondman.Interfaces;

    /// <summary>
    /// Base class for an IMDb object
    /// </summary>
    public class Reference : IDataObject
    {
        /// <summary>
        /// references the active sessions (if any)
        /// </summary>
        internal Session session;

        /// <summary>
        /// Gets or sets the ID (IMDb const) for this object.
        /// </summary>
        /// <value>The ID.</value>
        public string ID { get; set; }

        #region IDataObject members

        string IDataObject.PrimaryKey
        {
            get
            {
                return ID;
            }
            set
            {
                ID = value;
            }
        }

        #endregion
    }
}
