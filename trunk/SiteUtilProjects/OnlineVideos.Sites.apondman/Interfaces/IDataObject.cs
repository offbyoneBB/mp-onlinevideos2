namespace OnlineVideos.Sites.Pondman.Interfaces
{
    public interface IDataObject
    {
        /// <summary>
        /// Unique Identifier for the data object
        /// </summary>
        string PrimaryKey { get; set; }
    }
}