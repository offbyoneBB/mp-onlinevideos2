using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Entities
{
    /// <summary>
    /// The result of a specific event
    /// </summary>
    public class EventResult
    {
        public EventResultType Result { get; set; }
        public string ErrorMessage { get; set; }

        public static EventResult Complete()
        {
            return new EventResult { Result = EventResultType.Complete };
        }

        public static EventResult Error(string message)
        {
            return new EventResult { Result = EventResultType.Error, ErrorMessage = message };
        }

        public static EventResult Warning(string message)
        {
            return new EventResult { Result = EventResultType.Warning, ErrorMessage = message };
        }

    }
}
