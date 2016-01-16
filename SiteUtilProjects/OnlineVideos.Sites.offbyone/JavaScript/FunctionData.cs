using System;

namespace OnlineVideos.JavaScript
{
    public enum FunctionTypes
    {
        Undefined,
        Join,
        List,
        Slice,
        Splice,
        Reverse,
        Swap
    }

    public class FunctionData
    {
        public string Name { get; set; }

        public string[] Parameters { get; set; }

        public string Body { get; set; }

        public FunctionTypes Type { get; set; }
    }
}
