//using MediaPortal.InputDevices;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling.RemoteImplementations
{
    /// <summary>
    /// Remote handling specific to MediaPortal 1 - this will use reflection to load the RemotePlugins dll and initialise the InputDevices
    /// </summary>
    public class MediaPortal1RemoteHandling//: RemoteHandlingBase
    {
        /*
        /// <summary>
        /// CTor
        /// </summary>
        /// <param name="logger"></param>
        public MediaPortal1RemoteHandling(ILog logger)
            : base(logger)
        { 
        }

        
        /// <summary>
        /// Connect to the web service and attach the action handler
        /// </summary>
        public override void Initialise()
        {
            _logger.Info("Initialising Remote handling");
            InputDevices.Init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override bool ProcessWndProc(Message msg)
        {
            _logger.Info(string.Format("MediaPortal1RemoteHandling - WndProc message to be processed {0}, appCommand {1}, LParam {2}, WParam {3}", msg.Msg, GET_APPCOMMAND_LPARAM(msg.LParam), msg.LParam, msg.WParam));
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
                {
                    OnNewAction(action);
                }

                if (keyCode != Keys.A)
                {
                    var ke = new KeyEventArgs(keyCode);
                    OnKeyDown(ke);
                }
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
            _logger.Debug("MediaPortal1RemoteHandling - ProcessKeyPress {0}", keyPressed);

            Action action = new Action();
            //Try and get corresponding Action from key.
            //Some actions are mapped to KeyDown others to KeyPressed, try and handle both
            if (ActionTranslator.GetAction(-1, new Key(0, keyPressed), ref action))
            {
                OnNewAction(action);
            }
            else
            {
                //See if it's mapped to KeyPressed instead
                if (keyPressed >= (int)Keys.A && keyPressed <= (int)Keys.Z)
                    keyPressed += 32; //convert to char code
                if (ActionTranslator.GetAction(-1, new Key(keyPressed, 0), ref action))
                    OnNewAction(action);
            }
        }

*/
    }
}
