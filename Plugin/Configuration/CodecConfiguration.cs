using System;
using System.IO;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Diagnostics;

namespace OnlineVideos
{
    public class CodecConfiguration
    {
        public struct Codec
        {
            public string CLSID;
            public string Name;
            public string[] FileTypes;
            public string CodecFile;
            public string Version;
            public bool IsInstalled;
        }

        public Codec MPC_HC_FLVSplitter = new Codec() { CLSID = "{47E792CF-0BBE-4F7A-859C-194B0768650A}", Name = "MPC-HC FLV Splitter", FileTypes = new string[] { ".flv" } };
        public Codec MPC_HC_MP4Splitter = new Codec() { CLSID = "{61F47056-E400-43D3-AF1E-AB7DFFD4C4AD}", Name = "MPC-HC MP4 Splitter", FileTypes = new string[] { ".mp4", ".m4v", ".mov" } };
        public Codec WM_ASFReader = new Codec() { CLSID = "{187463A0-5BB7-11D3-ACBE-0080C75E246E}", Name = "WM ASF Reader", FileTypes = new string[] { ".wmv" } };
        public Codec AVI_Splitter = new Codec() { CLSID = "{1B544C20-FD0B-11CE-8C63-00AA0044B51E}", Name = "AVI Splitter", FileTypes = new string[] { ".avi" } };        

        public CodecConfiguration()
        {
            CheckCodec(ref MPC_HC_FLVSplitter);
            CheckCodec(ref MPC_HC_MP4Splitter);
            CheckCodec(ref WM_ASFReader);
            CheckCodec(ref AVI_Splitter);
        }

        void CheckCodec(ref Codec codec)
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + codec.CLSID + @"\InprocServer32", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                if (key != null)
                {
                    codec.CodecFile = key.GetValue("", null).ToString();
                    if (!Path.IsPathRooted(codec.CodecFile))
                    {
                        string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        codec.CodecFile = Path.Combine(systemPath, codec.CodecFile);
                    }
                    if (System.IO.File.Exists(codec.CodecFile))
                    {                        
                        codec.Version = FileVersionInfo.GetVersionInfo(codec.CodecFile).FileVersion.Replace(',', '.');
                        codec.IsInstalled = true;
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
