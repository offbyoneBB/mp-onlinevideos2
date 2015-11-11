using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.M3U
{
    namespace M3U
    {
        internal static class Constantes
        {
            internal static string M3U_START_MARKER = "#EXTM3U";
            internal static string M3U_INFO_MARKER = "EXTINF";
            internal static string M3U8_INFO_MARKER = "EXT-X-STREAM-INF";
        }

        internal static class Helper
        {
            internal static string GetBasePath(string path)
            {
                string basepath = path.Substring(0, path.LastIndexOf('/'));
                return basepath;
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

            internal static M3UElement[] ReadM3uElement(string content, string basepath)
            {
                List<M3UElement> tReturn = new List<M3UElement>();

                string[] tcontent = content.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);


                for (int idx = 1; idx < tcontent.Length; idx++)
                {
                    string sline = tcontent[idx];
                    string snextline = string.Empty;
                    if (idx + 1 < tcontent.Length)
                    {
                        snextline = tcontent[idx + 1];
                    }

                    if (sline.StartsWith(Constantes.M3U_INFO_MARKER) && (string.IsNullOrEmpty(snextline) || snextline.StartsWith(Constantes.M3U_INFO_MARKER)))
                    {
                        tReturn.Add(M3UElement.Read(sline, basepath));

                    }
                    else if (sline.StartsWith(Constantes.M3U8_INFO_MARKER) && (string.IsNullOrEmpty(snextline) || snextline.StartsWith(Constantes.M3U8_INFO_MARKER)))
                    {
                        tReturn.Add(M3U8Element.Read(sline, basepath));
                    }
                    else
                    {
                        //Detection INFOS
                        string[] tkeyvalue = sline.Split(':');
                        if (tkeyvalue.Length == 2)
                        {
                            // this.Options.Add(tkeyvalue[0], tkeyvalue[1]);
                        }
                        sline = string.Empty;
                    }
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
        }

        internal interface IM3UComponent
        {
            string Path { get; set; }
        }

        internal abstract class M3UComponent : List<IM3UComponent>, IM3UComponent
        {

            /// <summary>
            ///  Constructor
            /// </summary>
            public M3UComponent()
            {
            }

            public string Path { get; set; }
        }

        internal class M3UPlaylist : List<M3UElement>, IM3UComponent
        {

            /// <summary>
            /// Constructor
            /// </summary>
            public M3UPlaylist()
                : base()
            {
            }

            public void Read(string path)
            {
                this.Path = path;
                string basepath = string.Empty;

                string sContent = string.Empty;
                if (path.StartsWith("http"))
                {
                    sContent = Helper.GetWebData(path);
                    basepath = Helper.GetBasePath(path);
                }
                else
                {
                    // content = new StreamReader(path);
                }

                this.AddRange(Helper.ReadM3uElement(sContent, basepath));
            }


            public string Path { get; set; }

            public IEnumerable<M3UElement> OrderBy(string option)
            {
                IEnumerable<M3UElement> tReturn = null;
                tReturn = from item in this
                          orderby item.Options[option] ascending
                          select item;

                return tReturn;
            }
        } //EOC

        internal class M3U8Element : M3UElement
        {
            #region<<STATIC>>
            internal static M3U8Element Read(string content, string basepath)
            {
                M3U8Element oReturn = new M3U8Element();

                string[] tContent = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (tContent.Length > 1)
                {
                    oReturn.Path = tContent[tContent.Length - 1];

                    Helper.ConvertRelative2Absolute(basepath, oReturn);
                }

                string[] tOptions = tContent[0].Replace(Constantes.M3U8_INFO_MARKER + ":", "").Split(',');
                oReturn.Options = new System.Collections.Hashtable();

                foreach (string opt in tOptions)
                {
                    string[] tkeyvalue = opt.Split('=');
                    if (tkeyvalue.Length == 2)
                    {
                        oReturn.Options.Add(tkeyvalue[0], tkeyvalue[1]);
                    }
                }
                if (!string.IsNullOrEmpty(oReturn.Path))
                {
                    if (oReturn.Path.ToLower().Contains("m3u8"))
                    {
                        string sChilds = Helper.GetWebData(oReturn.Path);

                        List<M3UComponent> childs = new List<M3UComponent>();
                        childs.AddRange(Helper.ReadM3uElement(sChilds, basepath));
                        if (childs.Count > 0) oReturn.AddRange(childs);
                    }
                }
                else { oReturn = null; }

                return oReturn;
            }
            #endregion<<STATIC>>

            /// <summary>
            /// Constructor
            /// </summary>
            public M3U8Element()
                : base()
            {
            }

        } //EOC

        internal class M3UElement : M3UComponent
        {
            #region<<STATIC>>
            internal static M3UElement Read(string content, string basepath)
            {
                M3UElement oReturn = new M3UElement();

                string[] tContent = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

                string[] tOptions = tContent[0].Replace(Constantes.M3U_INFO_MARKER + ":", "").Split(',');
                oReturn.Options = new System.Collections.Hashtable();

                for (int idx = 0; idx < tOptions.Length; idx++)
                {
                    if (idx == 0)
                    {
                        oReturn.Options.Add("LENGHT", tOptions[idx]);
                    }
                    else if (idx == 1)
                    {
                        oReturn.Options.Add("INFOS", tOptions[idx]);
                    }
                }
                if (oReturn.Path.ToLower().Contains("m3u"))
                {
                    string sChilds = Helper.GetWebData(oReturn.Path);

                    List<M3UComponent> childs = new List<M3UComponent>();
                    string sbasepath = Helper.GetBasePath(oReturn.Path);
                    childs.AddRange(Helper.ReadM3uElement(sChilds, sbasepath));
                    if (childs.Count > 0) oReturn.AddRange(childs);

                }

                return oReturn;
            }
            #endregion<<STATIC>>

            /// <summary>
            /// Constructor
            /// </summary>
            public M3UElement()
                : base()
            {
                Options = new System.Collections.Hashtable();
            }

            public System.Collections.Hashtable Options { get; set; }

        } //EOC

    }
}
