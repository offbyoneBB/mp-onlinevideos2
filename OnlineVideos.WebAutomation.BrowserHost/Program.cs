using OnlineVideos.Sites.WebAutomation.BrowserHost.Factories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost
{
    static class Program
    {
        private static string _assemblyPath = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
       
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Process requires path to MediaPortal, Video Id, Web Automation Type, Username, Password
                if (args.Length < 5) return;

                //AppDomain.CurrentDomain.AppendPrivatePath(args[0]);
                _assemblyPath = args[0];
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

                // Set the BaseDirectory of the app domain to the mediaportal root, otherwise the inputdevices can't initialise (due to Configuration.Config using this path)
                // The other option is to run this exe in the root of the media portal dir, which I wasn't too keen on
                currentDomain.SetData("APPBASE", _assemblyPath); 

                var result = args[2];
                var host = new BrowserHost();
                IERegistryVersion.SetIEVersion();
                Application.Run(host.PlayVideo(result, args[1], args[3], args[4]));

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
                Console.Error.Flush();
            }
        }

        /// <summary>
        /// Need to resolve some assemblies from the root MediaPortal dir (passed in as a command line arg)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            Assembly MyAssembly = null;
            Assembly objExecutingAssemblies;
            string strTempAssmbPath = "";

            objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                var dllName = args.Name.Substring(0, args.Name.IndexOf(","));

                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == dllName)
                {
                    var fullDllName = dllName + ".dll";
                    
                    if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), fullDllName)))
                        strTempAssmbPath = Path.Combine(Directory.GetCurrentDirectory(), fullDllName);
                    else
                        //Build the path of the assembly from where it has to be loaded.				
                        strTempAssmbPath = Path.Combine(_assemblyPath, fullDllName);
                    break;
                }

            }
            if (!string.IsNullOrEmpty(strTempAssmbPath))
                //Load the assembly from the specified path. 					
                MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return MyAssembly;
        }

    }
}
