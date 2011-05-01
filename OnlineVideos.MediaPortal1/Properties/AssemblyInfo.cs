using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !MP11
using MediaPortal.Common.Utils;
#endif
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("OnlineVideos.MediaPortal1")]
[assembly: AssemblyDescription("OnlineVideos GUI FrontEnd for MediaPortal 1")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("OnlineVideos")]
[assembly: AssemblyCopyright("Copyright ©  2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("09b42e26-4a7f-46a7-b0a9-adda8a5099b0")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("0.30.0.0")]
[assembly: AssemblyFileVersion("0.30.0.0")]

#if !MP11
[assembly: CompatibleVersion("1.1.6.27644")]
[assembly: UsesSubsystem("MP.SkinEngine")]
[assembly: UsesSubsystem("MP.Players.Video")]
[assembly: UsesSubsystem("MP.Input")]
[assembly: UsesSubsystem("MP.Externals.SQLite")]
[assembly: UsesSubsystem("MP.Config")]
#endif