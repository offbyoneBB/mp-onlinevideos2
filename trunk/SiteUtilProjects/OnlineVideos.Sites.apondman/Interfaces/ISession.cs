using System;

namespace OnlineVideos.Sites.Pondman.Interfaces
{
    using ITunes;

    public interface ISession
    {
        Configuration Config { get; set; }

        Func<string, string> MakeRequest { get; set; }

        T Get<T>(string uri) where T : class, IExternalContentNode, new();
    }
}
