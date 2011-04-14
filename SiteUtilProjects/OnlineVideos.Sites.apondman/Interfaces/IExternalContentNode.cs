
namespace OnlineVideos.Sites.Pondman.Interfaces
{
    using OnlineVideos.Sites.Pondman.Nodes;

    public interface IExternalContentNode
    {
        /// <summary>
        /// Unique Resource Identifier
        /// </summary>
        string Uri { get; set; }

        /// <summary>
        /// Session that was used to instantiate the item
        /// </summary>
        ISession Session { get; set; }

        /// <summary>
        /// State of content node
        /// </summary>
        NodeState State { get; }

        /// <summary>
        /// Updates the object using the external content source
        /// </summary>
        /// <returns></returns>
        NodeResult Update();

    }
}
