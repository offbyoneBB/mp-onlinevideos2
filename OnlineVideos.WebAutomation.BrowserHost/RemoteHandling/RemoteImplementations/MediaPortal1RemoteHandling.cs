using MediaPortal.GUI.Library;
using MediaPortal.InputDevices;
using Action = MediaPortal.GUI.Library.Action;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling.RemoteImplementations
{
    /// <summary>
    /// Remote handling specific to MediaPortal 1 - this will use reflection to load the RemotePlugins dll and initialise the InputDevices
    /// </summary>
    public class MediaPortal1RemoteHandling: RemoteHandlingBase
    {
        
        /// <summary>
        /// CTor
        /// </summary>
        /// <param name="logger"></param>
        public MediaPortal1RemoteHandling(ILog logger)
            : base(logger)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.SetData("APPBASE", Directory.GetCurrentDirectory()); 
        }

        
        /// <summary>
        /// Connect to the web service and attach the action handler
        /// </summary>
        public override void Initialise()
        {
            _logger.Debug("Initialising Remote handling");

            try
            {
                //Load keyboard mappings
                ActionTranslator.Load();
                //Load remote mappings
                InputDevices.Init();
                GUIGraphicsContext.OnNewAction += OnNewAction;

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                if (ex.InnerException != null)
                    _logger.Error(ex.InnerException);
            }
        }

        /// <summary>
        /// Event handler for InputDevices
        /// </summary>
        /// <param name="action"></param>
        private void OnNewAction(Action action)
        {
            OnNewActionFromClient(action.wID.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override bool ProcessWndProc(Message msg)
        {
            _logger.Debug(string.Format("MediaPortal1RemoteHandling - WndProc message to be processed {0}, appCommand {1}, LParam {2}, WParam {3}", msg.Msg, ProcessHelper.GetLparamToAppCommand(msg.LParam), msg.LParam, msg.WParam));
            Action action;
            char key;
            Keys keyCode;

            if (msg.Msg == ProcessHelper.WM_COPYDATA)
            {
                var messageString = ProcessHelper.ReadStringFromMessage(msg);
                OnNewActionFromClient(messageString);
                return true;
            }

            if (InputDevices.WndProc(ref msg, out action, out key, out keyCode))
            {
                //If remote doesn't fire event directly we manually fire it
                if (action != null && action.wID != Action.ActionType.ACTION_INVALID)
                    OnNewActionFromClient(action.wID.ToString());

                if (keyCode != Keys.A)
                    ProcessKeyPress((int)keyCode);
                return true; // abort WndProc()
            }
            return false;
        }

        /// <summary>
        /// Process key press events
        /// </summary>
        /// <param name="keyPressed"></param>
        public override void ProcessKeyPress(int keyPressed)
        {
            _logger.Debug("MediaPortal1RemoteHandling - ProcessKeyPress {0} {1}", keyPressed, ((Keys)keyPressed).ToString());

            Action action = new Action();
            //Try and get corresponding Action from key.
            //Some actions are mapped to KeyDown others to KeyPressed, try and handle both
            if (ActionTranslator.GetAction(-1, new Key(0, keyPressed), ref action))
                OnNewActionFromClient(action.wID.ToString());
            else
            {
                //See if it's mapped to KeyPressed instead
                if (keyPressed >= (int)Keys.A && keyPressed <= (int)Keys.Z)
                    keyPressed += 32; //convert to char code
                if (ActionTranslator.GetAction(-1, new Key(keyPressed, 0), ref action))
                    OnNewActionFromClient(action.wID.ToString());
            }
        }

        /// <summary>
        /// Resolve the dlls for the MP1 remote handling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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

                    strTempAssmbPath = Path.Combine(Directory.GetCurrentDirectory(), fullDllName);

                    if (!File.Exists(strTempAssmbPath))
                        strTempAssmbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins\\windows\\onlinevideos", fullDllName);

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
