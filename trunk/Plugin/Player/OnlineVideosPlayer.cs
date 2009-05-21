using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.Configuration;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using System.Runtime.InteropServices;
using System.Drawing;

namespace OnlineVideos.Player
{
    public class OnlineVideosPlayer : VideoPlayerVMR9
    {
        public OnlineVideosPlayer()
            : base(g_Player.MediaType.Video)
        { }

        public OnlineVideosPlayer(g_Player.MediaType type)
            : base(type)
        { }

        protected override bool GetInterfaces()
        {
            DsRect rect = new DsRect();
            rect.top = 0;
            rect.bottom = GUIGraphicsContext.form.Height;
            rect.left = 0;
            rect.right = GUIGraphicsContext.form.Width;
            try
            {
                int num;
                bool flag2;
                bool flag3;
                IBaseFilter filter2;
                base.graphBuilder = (IGraphBuilder)new FilterGraph();
                bool flag = false;
                string strFilterName = "";
                string str2 = "";
                string str3 = "";
                string str4 = "";
                string str5 = "";
                int num2 = 0;
                string str6 = "";
                using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    flag = settings.GetValueAsBool("movieplayer", "autodecodersettings", false);
                    strFilterName = settings.GetValueAsString("movieplayer", "mpeg2videocodec", "");
                    str2 = settings.GetValueAsString("movieplayer", "h264videocodec", "");
                    str3 = settings.GetValueAsString("movieplayer", "mpeg2audiocodec", "");
                    str4 = settings.GetValueAsString("movieplayer", "aacaudiocodec", "");
                    str5 = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                    flag2 = settings.GetValueAsBool("movieplayer", "wmvaudio", false);
                    flag3 = settings.GetValueAsBool("subtitles", "enabled", false);
                    for (int j = 0; settings.GetValueAsString("movieplayer", "filter" + j.ToString(), "undefined") != "undefined"; j++)
                    {
                        if (settings.GetValueAsBool("movieplayer", "usefilter" + j.ToString(), false))
                        {
                            str6 = str6 + settings.GetValueAsString("movieplayer", "filter" + j.ToString(), "undefined") + ";";
                            num2++;
                        }
                    }
                }
                List<string> list = new List<string>();
                if (!flag)
                {
                    Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
                    GUIMessage message = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
                    GUIWindowManager.SendMessage(message);
                    this.Vmr9 = new VMR9Util();
                    this.Vmr9.AddVMR9(base.graphBuilder);
                    this.Vmr9.Enable(false);
                    string str7 = Path.GetExtension(base.m_strCurrentFile).ToLower();
                    if ((str7.Equals(".dvr-ms") || str7.Equals(".mpg")) || ((str7.Equals(".mpeg") || str7.Equals(".bin")) || str7.Equals(".dat")))
                    {
                        if (strFilterName.Length > 0)
                        {
                            base.videoCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, strFilterName);
                        }
                        if (str3.Length > 0)
                        {
                            base.audioCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, str3);
                        }
                    }
                    if (str7.Equals(".wmv"))
                    {
                        base.videoCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "WMVideo Decoder DMO");
                        base.audioCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "WMAudio Decoder DMO");
                    }
                    if (str7.Equals(".mp4") || str7.Equals(".mkv"))
                    {
                        if (str2.Length > 0)
                        {
                            base.h264videoCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, str2);
                        }
                        if (str3.Length > 0)
                        {
                            base.audioCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, str3);
                        }
                        if (str4.Length > 0)
                        {
                            try
                            {
                                base.aacaudioCodecFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, str4);
                            }
                            catch (Exception aacEx) { Log.Error(aacEx); }
                        }
                    }
                }
                else
                {
                    IEnumFilters filters;
                    num = base.graphBuilder.RenderFile(base.m_strCurrentFile, string.Empty);
                    IBaseFilter filter = null;
                    new ArrayList();
                    int num4 = base.graphBuilder.EnumFilters(out filters);
                    do
                    {
                        int num5;
                        IBaseFilter[] ppFilter = new IBaseFilter[1];
                        num4 = filters.Next(1, ppFilter, out num5);
                        if ((num4 == 0) && (num5 > 0))
                        {
                            FilterInfo info;
                            ppFilter[0].QueryFilterInfo(out info);
                            if (ppFilter[0] is IBasicVideo2)
                            {
                                filter = ppFilter[0];
                            }
                            else
                            {
                                DirectShowUtil.ReleaseComObject(ppFilter[0]);
                            }
                        }
                    }
                    while ((num4 == 0) && (filter == null));
                    DirectShowUtil.ReleaseComObject(filters);
                    if (filter != null)
                    {
                        IPin pin = DirectShowUtil.FindSourcePinOf(filter);
                        do
                        {
                            PinInfo info2;
                            FilterInfo info3;
                            pin.QueryPinInfo(out info2);
                            info2.filter.QueryFilterInfo(out info3);
                            DirectShowUtil.ReleaseComObject(pin);
                            pin = DirectShowUtil.FindSourcePinOf(info2.filter);
                            DirectShowUtil.ReleaseComObject(info2.filter);
                            if (pin != null)
                            {
                                list.Add(info3.achName);
                            }
                        }
                        while (pin != null);
                        if (base.graphBuilder != null)
                        {
                            while ((num = DirectShowUtil.ReleaseComObject(base.graphBuilder)) > 0)
                            {
                            }
                            base.graphBuilder = null;
                        }
                        base.graphBuilder = (IGraphBuilder)new FilterGraph();
                        Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
                        GUIMessage message2 = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
                        GUIWindowManager.SendMessage(message2);
                    }
                }
                if (str5.Length > 0)
                {
                    base.audioRendererFilter = DirectShowUtil.AddAudioRendererToGraph(base.graphBuilder, str5, false);
                }
                base.customFilters = new IBaseFilter[num2];
                string[] strArray = str6.Split(new char[] { ';' });
                for (int i = 0; i < num2; i++)
                {
                    base.customFilters[i] = DirectShowUtil.AddFilterToGraph(base.graphBuilder, strArray[i]);
                }
                base.graphBuilder.FindFilterByName("WMAudio Decoder DMO", out filter2);
                if ((filter2 != null) && flag2)
                {
                    Log.Info("VideoPlayerVMR9: Found WMAudio Decoder DMO", new object[0]);
                    object pVar = true;
                    num = ((IPropertyBag)filter2).Write("_HIRESOUTPUT", ref pVar);
                    if (num != 0)
                    {
                        Log.Info("VideoPlayerVMR9: Write failed: g_wszWMACHiResOutput {0}", new object[] { num });
                    }
                    else
                    {
                        Log.Info("VideoPlayerVMR9: WMAudio Decoder now set for > 2 audio channels", new object[0]);
                    }
                    DirectShowUtil.ReleaseComObject(filter2);
                }
                if (flag)
                {
                    this.Vmr9 = new VMR9Util();
                    this.Vmr9.AddVMR9(base.graphBuilder);
                    this.Vmr9.Enable(false);
                    foreach (string str8 in list)
                    {
                        DirectShowUtil.ReleaseComObject(DirectShowUtil.AddFilterToGraph(base.graphBuilder, str8));
                    }
                    num = base.graphBuilder.RenderFile(base.m_strCurrentFile, string.Empty);
                }
                else
                {
                    num = base.graphBuilder.RenderFile(base.m_strCurrentFile, string.Empty);
                }
                if (this.Vmr9 == null)
                {
                    Error.SetError("Unable to play movie", "Unable to render file. Missing codecs?");
                    Log.Error("VideoPlayer9: Failed to render file -> vmr9", new object[0]);
                    return false;
                }
                base.mediaCtrl = (IMediaControl)base.graphBuilder;
                base.mediaEvt = (IMediaEventEx)base.graphBuilder;
                base.mediaSeek = (IMediaSeeking)base.graphBuilder;
                base.mediaPos = (IMediaPosition)base.graphBuilder;
                base.basicAudio = base.graphBuilder as IBasicAudio;
                DirectShowUtil.EnableDeInterlace(base.graphBuilder);
                base.m_iVideoWidth = this.Vmr9.VideoWidth;
                base.m_iVideoHeight = this.Vmr9.VideoHeight;
                /*
                if (base.vob != null)
                {
                    Log.Info("VideoPlayerVMR9: release vob sub filter", new object[0]);
                    DirectShowUtil.ReleaseComObject(base.vob);
                    base.vob = null;
                }                
                Guid classID = new Guid("9852A670-F845-491B-9BE6-EBD841B8A613");
                DirectShowUtil.FindFilterByClassID(base.graphBuilder, classID, out this.vob);
                base.vobSub = null;
                base.vobSub = (IDirectVobSub)base.vob;
                if (base.vobSub == null)
                {
                    classID = new Guid("93A22E7A-5091-45ef-BA61-6DA26156A5D0");
                    DirectShowUtil.FindFilterByClassID(base.graphBuilder, classID, out this.vob);
                    base.vobSub = (IDirectVobSub)base.vob;
                }
                if (base.vobSub == null)
                {
                    Log.Info("VideoPlayerVMR9: no vob sub filter in the current graph", new object[0]);
                    if (flag3)
                    {
                        Log.Info("VideoPlayerVMR9: subtitles enabled - checking if subtitles are present", new object[0]);
                        bool flag4 = false;
                        string str9 = Path.ChangeExtension(base.m_strCurrentFile, null).ToLower();
                        if (File.Exists(str9 + ".srt") || File.Exists(str9 + ".sub"))
                        {
                            flag4 = true;
                        }
                        if (!flag4)
                        {
                            Log.Info("VideoPlayerVMR9: no compatible subtitles found", new object[0]);
                        }
                        else
                        {
                            Log.Info("VideoPlayerVMR9: subtitles present adding DirectVobSub filter to the current graph", new object[0]);
                            if (DirectShowUtil.AddFilterToGraph(base.graphBuilder, "DirectVobSub") == null)
                            {
                                Log.Info("VideoPlayerVMR9: DirectVobSub filter not found! You need to install DirectVobSub v2.39", new object[0]);
                                base.vobSub = null;
                            }
                            classID = new Guid("93A22E7A-5091-45ef-BA61-6DA26156A5D0");
                            Log.Info("VideoPlayerVMR9: add normal vob sub filter", new object[0]);
                            DirectShowUtil.FindFilterByClassID(base.graphBuilder, classID, out this.vob);
                            base.vobSub = (IDirectVobSub)base.vob;
                        }
                    }
                    else
                    {
                        Log.Info("VideoPlayerVMR9: subtitles are not enabled", new object[0]);
                    }
                }
                if (base.vobSub != null)
                {
                    using (Settings settings2 = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                    {
                        if (flag3)
                        {
                            int num10;
                            bool flag6;
                            bool flag7;
                            Log.Info("VideoPlayerVMR9: Setting DirectVobsub parameters", new object[0]);
                            string familyName = settings2.GetValueAsString("subtitles", "fontface", "Arial");
                            int num7 = settings2.GetValueAsInt("subtitles", "fontsize", 0x12);
                            bool flag5 = settings2.GetValueAsBool("subtitles", "bold", true);
                            long num8 = Convert.ToInt64(settings2.GetValueAsString("subtitles", "color", "ffffff"), 0x10);
                            int num9 = settings2.GetValueAsInt("subtitles", "shadow", 5);
                            LOGFONT lf = new LOGFONT();
                            bool fAdvancedRenderer = false;
                            int lflen = Marshal.SizeOf(typeof(LOGFONT));
                            base.vobSub.get_TextSettings(lf, lflen, out num10, out flag6, out flag7, out fAdvancedRenderer);
                            FontStyle regular = FontStyle.Regular;
                            if (flag5)
                            {
                                regular = FontStyle.Bold;
                            }
                            new Font(familyName, (float)num7, regular, GraphicsUnit.Point, 1).ToLogFont(lf);
                            int num12 = (int)((num8 >> 0x10) & 0xffL);
                            int num13 = (int)((num8 >> 8) & 0xffL);
                            int num14 = (int)(num8 & 0xffL);
                            num10 = ((num14 << 0x10) + (num13 << 8)) + num12;
                            if (num9 > 0)
                            {
                                flag6 = true;
                            }
                            base.vobSub.put_TextSettings(lf, lflen, num10, flag6, flag7, fAdvancedRenderer);
                            if (this.Vmr9.IsVMR9Connected)
                            {
                                IPin ppinIn = DsFindPin.ByDirection(base.vob, PinDirection.Input, 0);
                                IPin ppPin = null;
                                ppinIn.ConnectedTo(out ppPin);
                                if ((num != 0) || (ppPin == null))
                                {
                                    Log.Info("VideoPlayerVMR9: Connect vobsub's video pins!", new object[0]);
                                    ppPin = this.Vmr9.PinConnectedTo;
                                    this.Vmr9.Dispose();
                                    ppPin.Disconnect();
                                    if (base.graphBuilder.Connect(ppPin, ppinIn) != 0)
                                    {
                                        Log.Info("VideoPlayerVMR9: could not connect Vobsub's input video pin...", new object[0]);
                                        return false;
                                    }
                                    Log.Info("VideoPlayerVMR9: Vobsub's video input pin connected...", new object[0]);
                                    DirectShowUtil.ReleaseComObject(ppPin);
                                    this.Vmr9.AddVMR9(base.graphBuilder);
                                    this.Vmr9.Enable(false);
                                    ppPin = DirectShowUtil.FindPin(base.vob, PinDirection.Output, "Output");
                                    if (ppPin == null)
                                    {
                                        Log.Info("VideoPlayerVMR9: Vobsub output pin NOT FOUND!", new object[0]);
                                        return false;
                                    }
                                    num = base.graphBuilder.Render(ppPin);
                                    if (num != 0)
                                    {
                                        Log.Info("VideoPlayerVMR9: could not connect Vobsub to Vmr9 Renderer", new object[0]);
                                        return false;
                                    }
                                    Log.Info("VideoPlayerVMR9: Vobsub connected to Vmr9 Renderer...", new object[0]);
                                }
                                else
                                {
                                    DirectShowUtil.ReleaseComObject(ppPin);
                                }
                                DirectShowUtil.ReleaseComObject(ppinIn);
                                IPin pin4 = DirectShowUtil.FindPin(base.vob, PinDirection.Input, "Input");
                                if (pin4 != null)
                                {
                                    IPin pin5 = null;
                                    pin4.ConnectedTo(out pin5);
                                    if ((num != 0) || (pin5 == null))
                                    {
                                        Guid guid2 = new Guid("55DA30FC-F16B-49FC-BAA5-AE59FC65F82D");
                                        IBaseFilter filterFound = null;
                                        DirectShowUtil.FindFilterByClassID(base.graphBuilder, guid2, out filterFound);
                                        if (filterFound != null)
                                        {
                                            Log.Info("VideoPlayerVMR9: Connecting Haali's subtitle output to Vobsub's input.", new object[0]);
                                            pin5 = DirectShowUtil.FindPin(filterFound, PinDirection.Output, "Subtitle");
                                            if (pin5 != null)
                                            {
                                                IPin pin6 = null;
                                                pin5.ConnectedTo(out pin6);
                                                if (pin6 != null)
                                                {
                                                    pin5.Disconnect();
                                                    DirectShowUtil.ReleaseComObject(pin6);
                                                }
                                                num = base.graphBuilder.ConnectDirect(pin5, pin4, null);
                                                if (num != 0)
                                                {
                                                    Log.Info("VideoPlayerVMR9: Haali - Vobsub connect failed: {0}", new object[] { num });
                                                }
                                                DirectShowUtil.ReleaseComObject(pin5);
                                            }
                                            DirectShowUtil.ReleaseComObject(filterFound);
                                        }
                                    }
                                    else
                                    {
                                        DirectShowUtil.ReleaseComObject(pin5);
                                    }
                                    DirectShowUtil.ReleaseComObject(pin4);
                                }
                                base.vobSub.put_FileName(base.m_strCurrentFile);
                            }
                        }
                        else
                        {
                            PinInfo info4;
                            Log.Info("VideoPlayerVMR9: Subtitles are disabled but DirectVobSub is in the graph. Removing it accordingly", new object[0]);
                            IPin pin7 = DsFindPin.ByDirection(base.vob, PinDirection.Input, 0);
                            IPin pin8 = DsFindPin.ByDirection(base.vob, PinDirection.Input, 1);
                            IPin pin9 = null;
                            pin7.ConnectedTo(out pin9);
                            IPin pin10 = null;
                            pin8.ConnectedTo(out pin10);
                            if (pin9 == null)
                            {
                                Log.Info("VideoPlayerVMR9: DirectVobSub not connected, removing...", new object[0]);
                                if (pin10 != null)
                                {
                                    pin10.QueryPinInfo(out info4);
                                    if (pin10.Disconnect() != 0)
                                    {
                                        Log.Info("VideoPlayerVMR9: DirectVobSub failed disconnecting source subtitle output pin {0}", new object[] { info4.name });
                                    }
                                }
                                base.graphBuilder.RemoveFilter(base.vob);
                                while ((num = DirectShowUtil.ReleaseComObject(base.vobSub)) > 0)
                                {
                                }
                                base.vobSub = null;
                                while ((num = DirectShowUtil.ReleaseComObject(base.vob)) > 0)
                                {
                                }
                                base.vob = null;
                            }
                            else
                            {
                                pin9.QueryPinInfo(out info4);
                                Log.Info("VideoPlayerVMR9: DirectVobSub connected, removing...", new object[0]);
                                if (pin9.Disconnect() != 0)
                                {
                                    Log.Info("VideoPlayerVMR9: DirectVobSub failed disconnecting source video output pin: {0}", new object[] { info4.name });
                                }
                                if (pin10 != null)
                                {
                                    pin10.QueryPinInfo(out info4);
                                    if (pin10.Disconnect() != 0)
                                    {
                                        Log.Info("VideoPlayerVMR9: DirectVobSub failed disconnecting source subtitle output pin {0}", new object[] { info4.name });
                                    }
                                    DirectShowUtil.ReleaseComObject(pin8);
                                    DirectShowUtil.ReleaseComObject(pin10);
                                }
                                DirectShowUtil.ReleaseComObject(pin7);
                                this.Vmr9.Dispose();
                                base.graphBuilder.RemoveFilter(base.vob);
                                while ((num = DirectShowUtil.ReleaseComObject(base.vobSub)) > 0)
                                {
                                }
                                base.vobSub = null;
                                while ((num = DirectShowUtil.ReleaseComObject(base.vob)) > 0)
                                {
                                }
                                base.vob = null;
                                this.Vmr9.AddVMR9(base.graphBuilder);
                                this.Vmr9.Enable(false);
                                if (pin9 == null)
                                {
                                    Log.Info("VideoPlayerVMR9: Source output pin NOT FOUND!", new object[0]);
                                    return false;
                                }
                                num = base.graphBuilder.Render(pin9);
                                if (num != 0)
                                {
                                    Log.Info("VideoPlayerVMR9: Could not connect video out to video renderer: {0}", new object[] { num });
                                    return false;
                                }
                                Log.Info("VideoPlayerVMR9: Video out connected to video renderer...", new object[0]);
                                DirectShowUtil.ReleaseComObject(pin9);
                            }
                        }
                    }
                }
                */
                if (!this.Vmr9.IsVMR9Connected)
                {
                    base.mediaCtrl = null;
                    this.Cleanup();
                    return false;
                }
                this.Vmr9.SetDeinterlaceMode();
                return true;
            }
            catch (Exception exception)
            {
                Error.SetError("Unable to play movie", "Unable build graph for VMR9");
                Log.Error("VideoPlayer9:exception while creating DShow graph {0} {1}", new object[] { exception.Message, exception.StackTrace });
                return false;
            }
        }

        System.Reflection.MethodInfo cleanupMethod = null;
        private void Cleanup()
        {
            if (cleanupMethod == null) cleanupMethod = typeof(VideoPlayerVMR9).GetMethod("Cleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cleanupMethod.Invoke(this, null);
        }

        System.Reflection.FieldInfo vmr9Field = null;
        private VMR9Util Vmr9
        {
            get
            {
                if (vmr9Field == null) vmr9Field = typeof(VideoPlayerVMR9).GetField("Vmr9", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (VMR9Util)vmr9Field.GetValue(this);
            }
            set
            {
                if (vmr9Field == null) vmr9Field = typeof(VideoPlayerVMR9).GetField("Vmr9", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                vmr9Field.SetValue(this, value);
            }
        }

    }
}
