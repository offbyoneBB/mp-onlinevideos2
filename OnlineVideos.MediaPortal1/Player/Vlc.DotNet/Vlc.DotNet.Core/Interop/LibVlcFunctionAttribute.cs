using System;

namespace Vlc.DotNet.Core.Interops
{
    [AttributeUsage(AttributeTargets.Delegate, AllowMultiple = true)]
    internal sealed class LibVlcFunctionAttribute : Attribute
    {
        public string FunctionName { get; private set; }
        public Version MinVersion { get; private set; }
        public Version MaxVersion { get; private set; }

        public LibVlcFunctionAttribute(string functionName)
            : this(functionName, null)
        {
        }
        public LibVlcFunctionAttribute(string functionName, string minVersion)
            : this(functionName, minVersion, null)
        {
        }
        public LibVlcFunctionAttribute(string functionName, string minVersion, string maxVersion)
        {
            FunctionName = functionName;
            if (minVersion != null)
                MinVersion = new Version(minVersion);
            if (maxVersion != null)
                MaxVersion = new Version(maxVersion);
        }
    }
}
