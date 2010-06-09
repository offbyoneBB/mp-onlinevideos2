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
using System.Threading;

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

        public OnlineVideosPlayer(string url)
            : base(g_Player.MediaType.Video)
        {
            m_strCurrentFile = url;
        }

        protected override bool GetInterfaces()
        {
            string currentFileLower = CurrentFile.ToLower();

            /*if( currentFileLower.Contains(".avi") && !currentFileLower.Contains(".flv"))
                return BuildGraphForDivxStream();
            else*/
            
            if (graphBuilder != null) // graph was already started and playback file buffered
                return FinishPreparedGraph();
            else if (currentFileLower.StartsWith("mms://") || currentFileLower.Contains(".asf"))
                return BuildGraphForMMS();
            else if (currentFileLower.StartsWith("rtsp://"))
                return BuildGraphForRTSP();
            else
                return base.GetInterfaces();
        }      

        bool BuildGraphForDivxStream()
        {
            base.graphBuilder = (IGraphBuilder)new FilterGraph();
            // add video renderer
            Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
            GUIMessage message = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
            GUIWindowManager.SendMessage(message);
            this.Vmr9 = new VMR9Util();
            this.Vmr9.AddVMR9(base.graphBuilder);
            this.Vmr9.Enable(false);

            // add the audio renderer
            using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                DirectShowUtil.AddAudioRendererToGraph(base.graphBuilder, audiorenderer, false);
            }

            // add the source filter
            IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "Haali Media Splitter");
            if (sourceFilter == null) return false;
            int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
            if (result != 0) return false;

            IEnumPins enumPins;
            IPin[] sourceFilterPins = new IPin[2];
            int fetched;
            result = sourceFilter.EnumPins(out enumPins);
            result = enumPins.Next(2, sourceFilterPins, out fetched);
            // connect the pins automatically
            result = base.graphBuilder.Render(sourceFilterPins[0]); // audio
            result = base.graphBuilder.Render(sourceFilterPins[1]); // video

            // cleanup resources
            DirectShowUtil.ReleaseComObject(sourceFilter);
            DirectShowUtil.ReleaseComObject(enumPins);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[1]);

            // set fields for playback
            base.mediaCtrl = (IMediaControl)base.graphBuilder;
            base.mediaEvt = (IMediaEventEx)base.graphBuilder;
            base.mediaSeek = (IMediaSeeking)base.graphBuilder;
            base.mediaPos = (IMediaPosition)base.graphBuilder;
            base.basicAudio = base.graphBuilder as IBasicAudio;
            DirectShowUtil.EnableDeInterlace(base.graphBuilder);
            base.m_iVideoWidth = this.Vmr9.VideoWidth;
            base.m_iVideoHeight = this.Vmr9.VideoHeight;

            if (!this.Vmr9.IsVMR9Connected)
            {
                base.mediaCtrl = null;
                CloseInterfaces();
                return false;
            }
            this.Vmr9.SetDeinterlaceMode();

            return true;
        }

        bool BuildGraphForRTSP()
        {
            base.graphBuilder = (IGraphBuilder)new FilterGraph();
            // add video renderer
            Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
            GUIMessage message = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
            GUIWindowManager.SendMessage(message);
            this.Vmr9 = new VMR9Util();
            this.Vmr9.AddVMR9(base.graphBuilder);
            this.Vmr9.Enable(false);

            // add the audio renderer
            using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                DirectShowUtil.AddAudioRendererToGraph(base.graphBuilder, audiorenderer, false);
            }

            // add the source filter
            IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "MediaPortal File Reader");
            if (sourceFilter == null) return false;
            int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
            if (result != 0) return false;

            IEnumPins enumPins;
            IPin[] sourceFilterPins = new IPin[2];
            int fetched;
            result = sourceFilter.EnumPins(out enumPins);
            result = enumPins.Next(2, sourceFilterPins, out fetched);
            // connect the pins automatically
            result = base.graphBuilder.Render(sourceFilterPins[0]); // audio
            result = base.graphBuilder.Render(sourceFilterPins[1]); // video

            // cleanup resources
            DirectShowUtil.ReleaseComObject(sourceFilter);
            DirectShowUtil.ReleaseComObject(enumPins);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[1]);

            // set fields for playback
            base.mediaCtrl = (IMediaControl)base.graphBuilder;
            base.mediaEvt = (IMediaEventEx)base.graphBuilder;
            base.mediaSeek = (IMediaSeeking)base.graphBuilder;
            base.mediaPos = (IMediaPosition)base.graphBuilder;
            base.basicAudio = base.graphBuilder as IBasicAudio;
            DirectShowUtil.EnableDeInterlace(base.graphBuilder);
            base.m_iVideoWidth = this.Vmr9.VideoWidth;
            base.m_iVideoHeight = this.Vmr9.VideoHeight;

            if (!this.Vmr9.IsVMR9Connected)
            {
                base.mediaCtrl = null;
                CloseInterfaces();
                return false;
            }
            this.Vmr9.SetDeinterlaceMode();

            return true;
        }

        bool BuildGraphForMMS()
        {
            base.graphBuilder = (IGraphBuilder)new FilterGraph();
            _rotEntry = new DsROTEntry((IFilterGraph)graphBuilder);

            Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
            GUIMessage message = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
            GUIWindowManager.SendMessage(message);
            this.Vmr9 = new VMR9Util();
            this.Vmr9.AddVMR9(base.graphBuilder);
            this.Vmr9.Enable(false);

            // add the source filter manually
            IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "WM ASF Reader");

            if (sourceFilter == null)
            {
                Error.SetError("Unable to load DirectshowFilter: WM ASF Reader", "Windows Media Player not installed?");
                Log.Error("Unable to load DirectshowFilter: WM ASF Reader", new object[0]);
                return false;
            }

            // load the file with the source filter
            int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
            if (result != 0) return false;

            // mms streams allow skipping so set buffered to 100%
            PercentageBuffered = 100.0f;

            // add the audio renderer
            using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
            }

            // get the output pins of the WM ASF Reader
            IEnumPins enumPins;
            IPin[] sourceFilterPins = new IPin[2];
            int fetched;
            result = sourceFilter.EnumPins(out enumPins);
            result = enumPins.Next(2, sourceFilterPins, out fetched);
            // connect the pins automatically
            base.graphBuilder.Render(sourceFilterPins[0]);
            base.graphBuilder.Render(sourceFilterPins[1]);

            // cleanup resources
            DirectShowUtil.ReleaseComObject(sourceFilter);
            DirectShowUtil.ReleaseComObject(enumPins);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[1]);

            // set fields for playback
            base.mediaCtrl = (IMediaControl)base.graphBuilder;
            base.mediaEvt = (IMediaEventEx)base.graphBuilder;
            base.mediaSeek = (IMediaSeeking)base.graphBuilder;
            base.mediaPos = (IMediaPosition)base.graphBuilder;
            base.basicAudio = base.graphBuilder as IBasicAudio;
            DirectShowUtil.EnableDeInterlace(base.graphBuilder);
            base.m_iVideoWidth = this.Vmr9.VideoWidth;
            base.m_iVideoHeight = this.Vmr9.VideoHeight;

            if (!this.Vmr9.IsVMR9Connected)
            {
                base.mediaCtrl = null;
                CloseInterfaces();
                return false;
            }
            this.Vmr9.SetDeinterlaceMode();
            return true;
        }

        float PercentageBuffered { get;set;}

        public override void Process()
        {
            GUIPropertyManager.SetProperty("#TV.Record.percent3", PercentageBuffered.ToString());
            base.Process();
        }

        public bool StopBuffering { get; set; }

        void MonitorBufferProgress()
        {
            IAMOpenProgress sourceFilter = null;
            try
            {
                IBaseFilter filter = null;
                graphBuilder.FindFilterByName("File Source (URL)", out filter);
                sourceFilter = filter as IAMOpenProgress;
                Marshal.ReleaseComObject(filter);
                if (sourceFilter == null) return;

                int result = 0;
                long total = 0, current = 0, last = 0;
                do
                {
                    if (StopBuffering)
                    {
                        sourceFilter.AbortOperation();
                        break;
                    }
                    result = sourceFilter.QueryProgress(out total, out current);
                    PercentageBuffered = (float)current / (float)total * 100.0f;
                    if (current > last && current - last >= (double)total * 0.01) // log every percent
                    {
                        Log.Debug("Buffering: {0}/{1} KB ({2}%)", current / 1024, total / 1024, (int)PercentageBuffered);
                        GUIPropertyManager.SetProperty("#OnlineVideos.buffered", ((int)PercentageBuffered).ToString());
                        last = current;
                    }
                    Thread.Sleep(50); // no need to do this more often than 20 times per second
                }
                while (current < total && graphBuilder != null);
                PercentageBuffered = 100.0f;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                if (sourceFilter != null) Marshal.ReleaseComObject(sourceFilter);
            }
        }        

        /// <summary>
        /// If the url to be played can be buffered before starting playback, this function
        /// starts building a graph by adding the preferred video and audio render to it.
        /// This needs to be called on the MpMain Thread.
        /// </summary>
        /// <returns>true, if the url can be buffered</returns>
        public bool PrepareGraph()
        {
            Uri uri = new Uri(CurrentFile);
            if (uri.Scheme == "http")
            {
                graphBuilder = (IGraphBuilder)new FilterGraph();
                _rotEntry = new DsROTEntry((IFilterGraph)graphBuilder);
                
                Vmr9 = new VMR9Util();
                Vmr9.AddVMR9(graphBuilder);
                Vmr9.Enable(false);
                // set VMR9 back to NOT Active -> otherwise GUI is not refreshed while graph is building
                GUIGraphicsContext.Vmr9Active = false;                

                // add the audio renderer
                //using (Settings settings = new MPSettings()) // only available in 1.1+
                using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                    DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
                }

                return true;
            }
            else
            {
                return false;
            }
        }      

        /// <summary>
        /// This function can be called by a background thread. It finish building the graph and
        /// wait until the buffer is filled to the configured percentage.
        /// If a graph building requires the full file to be downloaded, the function will return when that is done.
        /// </summary>
        /// <returns>true, when playback can be started</returns>
        public bool BufferFile()
        {            
            // load the url with the source filter   
            IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, "File Source (URL)");
            int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
            if (result != 0) return false;
            // buffer before starting playback
            PercentageBuffered = 0.0f;
            new Thread(MonitorBufferProgress) { IsBackground = true, Name = "MonitorBufferProgress" }.Start();
            while (PercentageBuffered < OnlineVideoSettings.Instance.playbuffer) Thread.Sleep(50);
            // get the output pin of the source filter
            IEnumPins enumPins;
            IPin[] sourceFilterPins = new IPin[1];
            int fetched;
            result = sourceFilter.EnumPins(out enumPins);
            result = enumPins.Next(1, sourceFilterPins, out fetched);
            // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
            base.graphBuilder.Render(sourceFilterPins[0]);
            // cleanup resources
            DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
            DirectShowUtil.ReleaseComObject(enumPins);
            // playback is ready
            return true;
        }

        /// <summary>
        /// Third and last step of a graph build with the file source url filter used to monitor buffer.
        /// Needs to be called on the MpMain Thread.
        /// </summary>
        /// <returns></returns>
        bool FinishPreparedGraph()
        {
            try
            {
                if (Vmr9 == null || !Vmr9.IsVMR9Connected)
                {
                    Log.Error("OnlineVideosPlayer: Failed to render file -> vmr9");
                    mediaCtrl = null;
#if !MP102
                    Cleanup();
#endif
                    return false;
                }

                // now set VMR9 to Active
                GUIGraphicsContext.Vmr9Active = true;
                
                // set fields for playback
                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                mediaSeek = (IMediaSeeking)graphBuilder;
                mediaPos = (IMediaPosition)graphBuilder;
                basicAudio = (IBasicAudio)graphBuilder;
                videoWin = (IVideoWindow)graphBuilder;                
                m_iVideoWidth = Vmr9.VideoWidth;
                m_iVideoHeight = Vmr9.VideoHeight;
                
                Vmr9.SetDeinterlaceMode();
                return true;
            }
            catch (Exception ex)
            {
                Error.SetError("Unable to play movie", "Unable build graph for VMR9");
                Log.Error("OnlineVideosPlayer:exception while creating DShow graph {0} {1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        System.Reflection.FieldInfo vmr9Field = null;
        private new VMR9Util Vmr9
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

        public override bool Play(string strFile)
        {
            updateTimer = DateTime.Now;
            m_speedRate = 10000;
            m_bVisible = false;
            m_iVolume = 100;
            m_state = PlayState.Init;
            m_strCurrentFile = strFile;
            m_bFullScreen = true;
            m_ar = GUIGraphicsContext.ARType;
            VideoRendererStatistics.VideoState = VideoRendererStatistics.State.VideoPresent;
            _updateNeeded = true;
            Log.Info("VideoPlayer:play {0}", strFile);
            //lock ( typeof(VideoPlayerVMR7) )
            {
                //CloseInterfaces();
                m_bStarted = false;
                if (!GetInterfaces())
                {
                    m_strCurrentFile = "";
                    CloseInterfaces();
                    return false;
                }
                int hr = mediaEvt.SetNotifyWindow(GUIGraphicsContext.ActiveForm, WM_GRAPHNOTIFY, IntPtr.Zero);
                if (hr < 0)
                {
                    Error.SetError("Unable to play movie", "Can not set notifications");
                    m_strCurrentFile = "";
                    CloseInterfaces();
                    return false;
                }
                if (videoWin != null)
                {
                    videoWin.put_Owner(GUIGraphicsContext.ActiveForm);
                    videoWin.put_WindowStyle(
                      (WindowStyle)((int)WindowStyle.Child + (int)WindowStyle.ClipChildren + (int)WindowStyle.ClipSiblings));
                    videoWin.put_MessageDrain(GUIGraphicsContext.form.Handle);
                }
                if (basicVideo != null)
                {
                    hr = basicVideo.GetVideoSize(out m_iVideoWidth, out m_iVideoHeight);
                    if (hr < 0)
                    {
                        Error.SetError("Unable to play movie", "Can not find movie width/height");
                        m_strCurrentFile = "";
                        CloseInterfaces();
                        return false;
                    }
                }
                /*
                GUIGraphicsContext.DX9Device.Clear( ClearFlags.Target, Color.Black, 1.0f, 0);
                try
                {
                  // Show the frame on the primary surface.
                  GUIGraphicsContext.DX9Device.Present();
                }
                catch(DeviceLostException)
                {
                }*/
                DirectShowUtil.SetARMode(graphBuilder, AspectRatioMode.Stretched);
                // DsUtils.DumpFilters(graphBuilder);
                try
                {
                    hr = mediaCtrl.Run();
                    DsError.ThrowExceptionForHR(hr);
                }
                catch (Exception error)
                {
                    Log.Error("VideoPlayer: Unable to play with reason - {0}", error.Message);
                }
                if (hr < 0)
                {
                    Error.SetError("Unable to play movie", "Unable to start movie");
                    m_strCurrentFile = "";
                    CloseInterfaces();
                    return false;
                }
                GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
                msg.Label = strFile;
                GUIWindowManager.SendThreadMessage(msg);
                m_state = PlayState.Playing;
                //Brutus GUIGraphicsContext.IsFullScreenVideo=true;
                m_iPositionX = GUIGraphicsContext.VideoWindow.X;
                m_iPositionY = GUIGraphicsContext.VideoWindow.Y;
                m_iWidth = GUIGraphicsContext.VideoWindow.Width;
                m_iHeight = GUIGraphicsContext.VideoWindow.Height;
                m_ar = GUIGraphicsContext.ARType;
                _updateNeeded = true;
                SetVideoWindow();
                mediaPos.get_Duration(out m_dDuration);
                Log.Info("VideoPlayer:Duration:{0}", m_dDuration);
                AnalyseStreams();
                //SelectSubtitles();
                //SelectAudioLanguage();
                OnInitialized();
            }
            return true;
        }

        public override void Stop()
        {
            StopBuffering = true;
            Thread.Sleep(200);
            base.Stop();
        }
    }
}
