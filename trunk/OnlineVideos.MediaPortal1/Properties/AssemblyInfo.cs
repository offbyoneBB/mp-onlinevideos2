using System.Reflection;
using System.Runtime.InteropServices;
using MediaPortal.Common.Utils;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("OnlineVideos MediaPortal1 GUI")]
[assembly: AssemblyDescription("OnlineVideos GUI FrontEnd for MediaPortal 1")]
[assembly: AssemblyProduct("OnlineVideos")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("09b42e26-4a7f-46a7-b0a9-adda8a5099b0")]

// Define that our plugin is designed for MediaPortal 1.5 and compatible with at least 1.4
[assembly: CompatibleVersion("1.4.100.0", "1.3.100.0")]

// Tell MediaPortal which subsystems this plugin will use, so it can check for compatiblity
[assembly: UsesSubsystem("MP.SkinEngine")]
[assembly: UsesSubsystem("MP.Players.Video")]
[assembly: UsesSubsystem("MP.Input")]
[assembly: UsesSubsystem("MP.Externals.SQLite")]
[assembly: UsesSubsystem("MP.Externals.Log4Net")]
[assembly: UsesSubsystem("MP.Config")]
[assembly: UsesSubsystem("MP.Plugins.Videos")] // needed so plugin will appear incompatible with MP 1.7 where this was incremented due to new dlls