namespace Vlc.DotNet.Core
{
    /// <summary>
    /// VlcLogMessage class
    /// </summary>
    public sealed class VlcLogMessage
    {
        public VlcLogVerbosities Verbosity { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
        public string Header { get; private set; }
        public string Message { get; private set; }

        internal VlcLogMessage(int verbosity, string type, string name, string header, string message)
        {
            Verbosity = (VlcLogVerbosities) verbosity;
            Type = type;
            Name = name;
            Header = header;
            Message = message;
        }
    }
}
