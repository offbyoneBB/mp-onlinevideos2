using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace OnlineVideos
{
    class Program
    {
        public static bool RegKeyExists(string basePath)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(basePath);
            if (key == null)
                key = Registry.CurrentUser.OpenSubKey(basePath);
            return key != null && key.GetValue("pv") != null;
        }

        public static void Main(string[] args)
        {
            //https://go.microsoft.com/fwlink/p/?LinkId=2124703
            bool installed = RegKeyExists(@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}") ||
                RegKeyExists(@"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}");
            if (!installed)
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

                var fileName = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(@"https://go.microsoft.com/fwlink/p/?LinkId=2124703", fileName);
                    using (Process myProcess = new Process())
                    {
                        myProcess.StartInfo.UseShellExecute = false;
                        myProcess.StartInfo.FileName = fileName;
                        //myProcess.StartInfo.Arguments = MpeInstaller.TransformInRealPath(actionItem.Params[Const_Params].Value);
                        //myProcess.StartInfo.CreateNoWindow = true;
                        myProcess.Start();
                        myProcess.WaitForExit();
                    }
                }
                /*
      try
      {
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.FileName = MpeInstaller.TransformInRealPath(actionItem.Params[Const_APP].Value);
        myProcess.StartInfo.Arguments = MpeInstaller.TransformInRealPath(actionItem.Params[Const_Params].Value);
        myProcess.StartInfo.CreateNoWindow = true;

        if (packageClass.Silent)
        {
          myProcess.StartInfo.CreateNoWindow = true;
          myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        }
        myProcess.Start();
        if (actionItem.Params[Const_Wait].GetValueAsBool())
        {
          myProcess.WaitForExit();
          if (myProcess.ExitCode != 0)
            return SectionResponseEnum.Error;
        }
      }
      catch
      {
        if (ItemProcessed != null)
          ItemProcessed(this, new InstallEventArgs("Error to start application"));
        return SectionResponseEnum.Error;
      }
                FileItem item = new FileItem();
                packageClass.ZipProvider.Extract(item, destination);
                System.Diagnostics.Process.Start("CMD.exe", strCmdText);
                 */
            }


            /*
             * string satelliteDllsPath = Path.Combine(MpeInstaller.TransformInRealPath("%Plugins%"), "Windows\\OnlineVideos");

            // get the SecurityIdentifier object for the current user
            SecurityIdentifier userSid = System.Security.Principal.WindowsIdentity.GetCurrent().User;

            DirectorySecurity security = System.IO.Directory.GetAccessControl(satelliteDllsPath);
            FileSystemAccessRule newRule =
              new FileSystemAccessRule(
                userSid,
                FileSystemRights.FullControl,                                       // full control so no arm if new files are created
                InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, // all subfolders and files
                PropagationFlags.NoPropagateInherit,
                AccessControlType.Allow);
            bool modified = false;
            security.ModifyAccessRule(AccessControlModification.Add, newRule, out modified);
            System.IO.Directory.SetAccessControl(satelliteDllsPath, security);
            */
        }
    }
}
