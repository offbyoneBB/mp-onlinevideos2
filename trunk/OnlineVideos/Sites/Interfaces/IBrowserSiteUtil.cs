using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// All browser site utils must implement this interface
    /// </summary>
    public interface IBrowserSiteUtil
    {
        string ConnectorEntityTypeName { get; }
        string UserName { get; }
        string Password { get; }
    }
}
