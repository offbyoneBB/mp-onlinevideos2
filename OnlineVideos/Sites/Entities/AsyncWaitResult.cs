namespace OnlineVideos.Sites.Entities
{
    /// <summary>
    /// Result of an sasynchronous call
    /// </summary>
    public class AsyncWaitResult
    {
        public bool Finished { get; set; }
        public bool Success { get; set; }

        public AsyncWaitResult()
        {
            Finished = true;
        }
    }
}
