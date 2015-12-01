using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace OnlineVideos.Sites.M3U
{
    namespace M3U
    {
        internal static class Constantes
        {
            internal static string M3U_START_MARKER = "#EXTM3U";
            internal static string M3U_INFO_MARKER = "#EXTINF";
            internal static string M3U8_INFO_MARKER = "#EXT-X-STREAM-INF";

            internal static string INT_OPTIONS = "#EXT-X-MEDIA-SEQUENCE#EXT-X-TARGETDURATION#EXT-X-VERSION#EXT-X-MEDIA-SEQUENCE";
        } //EOC

        internal static class Helper
        {

            internal static string GetWebBasePath(string path)
            {
                string basepath = path.Substring(0, path.LastIndexOf('/'));
                return basepath;
            }

            internal static string GetLocalBasePath(string path)
            {
                return System.IO.Directory.GetParent(path).FullName;
            }

            internal static string GetWebData(string url)
            {
                string sReturn = string.Empty;
                try
                {
                    System.Net.HttpWebRequest webReq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                    Stream contentStream = webReq.GetResponse().GetResponseStream();
                    sReturn = new StreamReader(contentStream).ReadToEnd();
                }
                catch (System.Net.WebException) { }
                return sReturn;
            }

            internal static M3UElement[] ReadM3uElement(string content, string basepath, ReaderConfiguration config, int activeDepth)
            {
                string[] tcontent = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                return ReadM3uElement(tcontent, basepath, config, activeDepth);
            }

            internal static M3UElement[] ReadM3uElement(string[] tcontent, string basepath, ReaderConfiguration config, int activeDepth)
            {
                return ReadM3uElement(null, tcontent, basepath, config, activeDepth);
            }

            internal static M3UElement[] ReadM3uElement(M3UPlaylist m3UPlaylist, string[] tcontent, string basepath, ReaderConfiguration config, int activeDepth)
            {
                List<M3UElement> tReturn = new List<M3UElement>();

                List<string> tElement = new List<string>();
                int index = 0;

                while (index < tcontent.Length)
                {
                    string line = tcontent[index].Trim();
                    tElement.Add(line);
                    if (!line.StartsWith("#"))
                    {
                        //An M3Ux Element is found
                        string sline = tElement[0].Trim();
                        if (sline.StartsWith(Constantes.M3U_INFO_MARKER))
                        {
                            tReturn.Add(M3UElement.Read(tElement.ToArray(), basepath, config, activeDepth + 1));
                        }
                        else if (sline.StartsWith(Constantes.M3U8_INFO_MARKER))
                        {
                            tReturn.Add(M3U8Element.Read(tElement.ToArray(), basepath, config, activeDepth + 1));
                        }
                        tElement = new List<string>();
                    }
                    index++;
                }

                if (m3UPlaylist != null)
                {
                    m3UPlaylist.AddRange(tReturn.ToArray());
                }
                return tReturn.ToArray();
            }

            internal static void ConvertRelative2Absolute(string basepath, M3UComponent component)
            {
                if (basepath.StartsWith("http") && !component.Path.StartsWith("http"))
                {
                    component.Path = basepath + "/" + component.Path;
                }
            }

            internal static void ReadOptions(string optionLine, System.Collections.Hashtable Options, char splitterChar = ':')
            {
                optionLine = optionLine.Trim();
                int splitterPos = optionLine.IndexOf(splitterChar);
                if (splitterPos < 0) return;
                string key = optionLine.Substring(0, splitterPos);
                object value = optionLine.Substring(splitterPos + 1);

                if (Constantes.INT_OPTIONS.Contains(key))
                {
                    value = int.Parse(value.ToString());
                }

                if (!Options.ContainsKey(key))
                    Options.Add(key, value);
                else
                    Options[key] = value;

            }

        } //EOC

        /// <summary>
        /// Basic deffinition
        /// </summary>
        internal interface IM3UComponent
        {
            /// <summary>
            /// Absolute Path of the Element
            /// </summary>
            string Path { get; set; }

            /// <summary>
            /// Options of the Element
            /// </summary>
            System.Collections.Hashtable Options { get; set; }
        } //EOI

        /// <summary>
        /// Basic Implementation
        /// </summary>
        internal abstract class M3UComponent : List<IM3UComponent>, IM3UComponent
        {
            #region <<CTR>>
            /// <summary>
            ///  Constructor
            /// </summary>
            public M3UComponent()
            {
            }
            #endregion <<CTR>>

            /// <summary>
            /// Absolute Path of the Element
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// Options of the element
            /// </summary>
            public System.Collections.Hashtable Options { get; set; }

            public int Depth { get; set; }

            public override string ToString()
            {
                return this.Path;
            }

        } //EOC

        /// <summary>
        /// M3U Element
        /// </summary>
        internal class M3UElement : M3UComponent
        {
            #region<<STATIC>>
            internal static M3UElement Read(string[] tContent, string basepath, ReaderConfiguration config, int activeDepth)
            {
                M3UElement oReturn = new M3UElement(activeDepth);

                if (tContent.Length > 1)
                {
                    oReturn.Path = tContent[tContent.Length - 1];
                    Helper.ConvertRelative2Absolute(basepath, oReturn);
                }
                else if (tContent.Length == 1)
                {
                    oReturn.Path = tContent[0];
                    Helper.ConvertRelative2Absolute(basepath, oReturn);
                }

                oReturn.Options = new System.Collections.Hashtable();
                if (tContent[0].StartsWith(Constantes.M3U_INFO_MARKER))
                {
                    string[] tOptions = tContent[0].Replace(Constantes.M3U_INFO_MARKER + ":", "").Split(',');

                    if (tOptions.Length > 0)
                        oReturn.Options.Add("LENGHT", tOptions[0].Trim());

                    if (tOptions.Length > 1)
                        oReturn.Options.Add("INFOS", tOptions[1].Trim());

                }

                //Try Recursive.
                if (oReturn.Path.ToLower().Contains("m3u") && config.Depth > activeDepth)
                {
                    string sChilds = Helper.GetWebData(oReturn.Path);
                    string sbasepath = Helper.GetWebBasePath(oReturn.Path);

                    int contentPosition = 0;
                    string[] fullcontent = sChilds.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    //Analysing Playlist Options
                    for (int idx = 1; idx < fullcontent.Length; idx++)
                    {
                        string sline = fullcontent[idx].Trim();
                        if (sline.StartsWith("#EXT") && sline.Contains(":") && (!sline.StartsWith(Constantes.M3U8_INFO_MARKER) && !sline.StartsWith(Constantes.M3U_INFO_MARKER) && !sline.Contains("EXTVLCOPT")))
                        {
                            Helper.ReadOptions(sline, oReturn.Options);
                            contentPosition = idx;
                        }
                    }


                    List<M3UComponent> childs = new List<M3UComponent>();
                    string[] contentStreams = fullcontent.Where((x, idx) => idx > contentPosition).ToArray();
                    childs.AddRange(Helper.ReadM3uElement(contentStreams, sbasepath, config, 0));
                    if (childs.Count > 0) oReturn.AddRange(childs);

                }

                return oReturn;
            }

            internal static M3UElement Read(string content, string basepath, ReaderConfiguration config, int activeDepth)
            {
                string[] tContent = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return Read(tContent, basepath, config, activeDepth);
            }
            #endregion<<STATIC>>

            #region <<CTR>>
            /// <summary>
            /// Constructor
            /// </summary>
            public M3UElement()
                : base()
            {
                Options = new System.Collections.Hashtable();
            }

            protected M3UElement(int depth)
                : base()
            {
                this.Depth = depth;
                Options = new System.Collections.Hashtable();
            }
            #endregion <<CTR>>

        } //EOC

        /// <summary>
        /// M3U8 Element, much like a M3UElement
        /// </summary>
        internal class M3U8Element : M3UElement
        {
            #region<<STATIC>>
            internal static M3U8Element Read(string[] tContent, string basepath, ReaderConfiguration config, int activeDepth)
            {
                M3U8Element oReturn = new M3U8Element(activeDepth);

                if (tContent.Length > 1)
                {
                    oReturn.Path = tContent[tContent.Length - 1];

                    Helper.ConvertRelative2Absolute(basepath, oReturn);
                }

                string[] tOptions = tContent[0].Replace(Constantes.M3U8_INFO_MARKER + ":", "").Split(',');
                oReturn.Options = new System.Collections.Hashtable();

                foreach (string opt in tOptions)
                {
                    Helper.ReadOptions(opt, oReturn.Options, '=');
                }
                if (!string.IsNullOrEmpty(oReturn.Path))
                {
                    if (oReturn.Path.ToLower().Contains("m3u8") && config.Depth > activeDepth)
                    {
                        string sChilds = Helper.GetWebData(oReturn.Path);

                        List<M3UComponent> childs = new List<M3UComponent>();
                        childs.AddRange(Helper.ReadM3uElement(sChilds, basepath, config, activeDepth));
                        if (childs.Count > 0)
                            oReturn.AddRange(childs);
                    }
                }
                else { oReturn = null; }

                return oReturn;
            }

            internal new static M3U8Element Read(string content, string basepath, ReaderConfiguration config, int activeDepth)
            {
                string[] tContent = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return Read(tContent, basepath, config, activeDepth);
            }
            #endregion<<STATIC>>

            #region <<CTR>>
            /// <summary>
            /// Constructor
            /// </summary>
            public M3U8Element()
                : base()
            {
            }

            protected M3U8Element(int depth)
                : base()
            {
                this.Depth = depth;
            }
            #endregion <<CTR>>

        } //EOC

        /// <summary>
        /// M3U or M3U8 Playlist
        /// </summary>
        internal class M3UPlaylist : List<M3UElement>, IM3UComponent
        {

            #region <<DECLARATION>>
            private Thread _watcher = null;
            #endregion <<DECLARATION>>

            #region <<CTR>>
            /// <summary>
            /// Constructor
            /// </summary>
            public M3UPlaylist()
                : base()
            {
                Configuration = new ReaderConfiguration();
                Options = new System.Collections.Hashtable();
            }

            ~M3UPlaylist()
            {
                if (_watcher != null)
                {
                    try
                    {
                        _watcher.Abort();
                        _watcher = null;
                    }
                    catch { }
                }
            }
            #endregion <<CTR>>

            /// <summary>
            /// Absolute Path of the Element
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// Options
            /// </summary>
            public System.Collections.Hashtable Options { get; set; }

            /// <summary>
            /// Read the M3Ux Playlist
            /// </summary>
            /// <param name="path"></param>
            public void Read(string path)
            {
                this.Path = path;
                string basepath = string.Empty; //For converting relative path to absolute

                string sContent = string.Empty;
                if (path.StartsWith("http"))
                {
                    sContent = Helper.GetWebData(path);
                    basepath = Helper.GetWebBasePath(path);
                }
                else if (System.IO.File.Exists(path))
                {
                    sContent = System.IO.File.ReadAllText(path);
                    basepath = Helper.GetLocalBasePath(path);
                }

                AnalyzeContent(basepath, sContent);
            }

            /// <summary>
            /// To Order by an option
            /// </summary>
            /// <param name="option">option name</param>
            /// <returns></returns>
            public IEnumerable<M3UElement> OrderBy(string option)
            {
                IEnumerable<M3UElement> tReturn = null;
                tReturn = from item in this
                          orderby item.Options[option] ascending
                          select item;

                return tReturn;
            }

            /// <summary>
            /// Reader Configuration
            /// </summary>
            public ReaderConfiguration Configuration { get; set; }

            private void AnalyzeContent(string basepath, string content)
            {
                if (string.IsNullOrEmpty(content)) return;

                string[] fullcontent = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int contentPosition = 0;

                //Analysing Playlist Options
                for (int idx = 1; idx < fullcontent.Length; idx++)
                {
                    string sline = fullcontent[idx].Trim();
                    if (sline.StartsWith("#EXT") && sline.Contains(":") && (!sline.StartsWith(Constantes.M3U8_INFO_MARKER) && !sline.StartsWith(Constantes.M3U_INFO_MARKER) && !sline.Contains("EXTVLCOPT")))
                    {
                        Helper.ReadOptions(sline, this.Options);
                        contentPosition = idx;
                    }
                }

                //Analysing Stream
                string[] contentStreams = fullcontent.Where((x, idx) => idx > contentPosition).ToArray();
                Helper.ReadM3uElement(this, contentStreams, basepath, this.Configuration, 0);
            }

        } //EOC

        internal class ReaderConfiguration
        {
            /// <summary>
            /// Reading Depth.
            /// 0 means full recursive reading
            /// </summary>
            internal int Depth { get; set; }
        } //EOC
    }

    
}
