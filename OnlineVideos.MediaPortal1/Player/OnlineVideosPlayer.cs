using System;
using System.Threading;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;

#if !MP11
using MediaPortal.Player.Subtitles;
using MediaPortal.Player.PostProcessing;
#endif

namespace OnlineVideos.MediaPortal1.Player
{
    public class OnlineVideosPlayer : VideoPlayerVMR9, OVSPLayer
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
            if (graphBuilder != null) // graph was already started and playback file buffered
                return FinishPreparedGraph();
            else
                return base.GetInterfaces();
        }      

        float PercentageBuffered;

        DateTime lastProgressCheck = DateTime.MinValue;
        public override void Process()
        {
            if (PercentageBuffered < 100.0f && graphBuilder != null && (DateTime.Now - lastProgressCheck).TotalMilliseconds > 100)
            {
                lastProgressCheck = DateTime.Now;
                IBaseFilter sourceFilter = null;
                try
                {
                    int result = graphBuilder.FindFilterByName(PluginConfiguration.Instance.httpSourceFilterName, out sourceFilter);
                    if (result == 0)
                    {
                        long total = 0, current = 0;
                        ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                        PercentageBuffered = (float)current / (float)total * 100.0f;
                        GUIPropertyManager.SetProperty("#TV.Record.percent3", PercentageBuffered.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error Quering Progress: {0}", ex.Message);
                }
                finally
                {
                    if (sourceFilter != null) DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                }
            }
            base.Process();
        }

        public bool BufferingStopped { get; protected set; }
        public void StopBuffering() { BufferingStopped = true; }

        protected bool skipBuffering = false;
        public void SkipBuffering() { skipBuffering = true; }

        /// <summary>
        /// If the url to be played can be buffered before starting playback, this function
        /// starts building a graph by adding the preferred video and audio render to it.
        /// This needs to be called on the MpMain Thread.
        /// </summary>
        /// <returns>true, if the url can be buffered (a graph was started), false if it can't be and null if an error occured building the graph</returns>
        public bool? PrepareGraph()
        {
            Uri uri = new Uri(CurrentFile);
            string sourceFilterName = uri.Scheme == "http" ? PluginConfiguration.Instance.httpSourceFilterName : (uri.Scheme == "mms" || CurrentFile.ToLower().Contains(".asf")) ? "WM ASF Reader" : string.Empty;
            if (!string.IsNullOrEmpty(sourceFilterName))
            {
                graphBuilder = (IGraphBuilder)new FilterGraph();
                _rotEntry = new DsROTEntry((IFilterGraph)graphBuilder);

                Vmr9 = new VMR9Util();
                Vmr9.AddVMR9(graphBuilder);
                Vmr9.Enable(false);
                // set VMR9 back to NOT Active -> otherwise GUI is not refreshed while graph is building
                GUIGraphicsContext.Vmr9Active = false;

                // add the audio renderer
                using (Settings settings = new MPSettings())
                {
                    string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                    DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
                }

                // set fields for playback
                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                mediaSeek = (IMediaSeeking)graphBuilder;
                mediaPos = (IMediaPosition)graphBuilder;
                basicAudio = (IBasicAudio)graphBuilder;
                videoWin = (IVideoWindow)graphBuilder;

                // add the source filter
                IBaseFilter sourceFilter = null;
                try
                {
                    sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, sourceFilterName);
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error adding '{0}' filter to graph: {1}", sourceFilterName, ex.Message);
                    return null;
                }
                finally
                {
                    if (sourceFilter != null) DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                }
                return true;
            }
            else
            {
                return false;
            }
        }      

        /// <summary>
        /// This function can be called by a background thread. It finishes building the graph and
        /// waits until the buffer is filled to the configured percentage.
        /// If a filter in the graph requires the full file to be downloaded, the function will return only afterwards.
        /// </summary>
        /// <returns>true, when playback can be started</returns>
        public bool BufferFile()
        {
            VideoRendererStatistics.VideoState = VideoRendererStatistics.State.VideoPresent; // prevents the BlackRectangle on first time playback

            Uri uri = new Uri(CurrentFile);
            bool PlaybackReady = false;
            IBaseFilter sourceFilter = null;
            try
            {
                string sourceFilterName = uri.Scheme == "http" ? PluginConfiguration.Instance.httpSourceFilterName : (uri.Scheme == "mms" || CurrentFile.ToLower().Contains(".asf")) ? "WM ASF Reader" : string.Empty;
                int result = graphBuilder.FindFilterByName(sourceFilterName, out sourceFilter);
                if (result != 0)
                {
                    Log.Instance.Warn("BufferFile : FindFilterByName returned {0}", result);
                    return false;
                }
                
                // translate url if usage of rtmp proxy is not wanted
                string urlToLoad = CurrentFile;
                if (uri.Scheme == "http" && !PluginConfiguration.Instance.useRtmpProxy)
                {
                    string proxyIndicator = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance, "rtmp://");
                    if (CurrentFile.StartsWith(proxyIndicator))
                    {
                        urlToLoad = Uri.UnescapeDataString("rtmp://" + CurrentFile.Replace(proxyIndicator, ""));
                    }
                }

                result = ((IFileSourceFilter)sourceFilter).Load(urlToLoad, null);

                if (result != 0)
                {
                    Log.Instance.Warn("BufferFile : IFileSourceFilter.Load returned '{0}' ({1})", result, DirectShowLib.DsError.GetErrorText(result));
                    return false;
                }

                if (sourceFilter is IAMOpenProgress && !CurrentFile.Contains("live=true"))
                {
                    // buffer before starting playback
                    bool filterConnected = false;
                    PercentageBuffered = 0.0f;
                    long total = 0, current = 0, last = 0;
                    do
                    {
                        result = ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                        PercentageBuffered = (float)current / (float)total * 100.0f;
                        // after configured percentage has been buffered, connect the graph
                        if (!filterConnected && (PercentageBuffered >= PluginConfiguration.Instance.playbuffer || skipBuffering))
                        {
                            if (skipBuffering) Log.Instance.Debug("Buffering skipped at {0}%", PercentageBuffered);
                            filterConnected = true;
                            new Thread(delegate()
                            {
                                try
                                {
                                    // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
                                    DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                                    PlaybackReady = true;
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Warn(ex.Message);
                                    StopBuffering();
                                }
                            }) { IsBackground = true }.Start();
                        }
                        // log every percent
                        if (current > last && current - last >= (double)total * 0.01)
                        {
                            Log.Instance.Debug("Buffering: {0}/{1} KB ({2}%)", current / 1024, total / 1024, (int)PercentageBuffered);
                            last = current;
                        }
                        // set the percentage to a gui property, formatted according to percentage, so the user knows very early if anything is buffering                   
                        string formatString = "###";
                        if (PercentageBuffered == 0f) formatString = "0.0";
                        else if (PercentageBuffered < 1f) formatString = ".00";
                        else if (PercentageBuffered < 10f) formatString = "0.0";
                        else if (PercentageBuffered < 100f) formatString = "##";
                        GUIPropertyManager.SetProperty("#OnlineVideos.buffered", PercentageBuffered.ToString(formatString, System.Globalization.CultureInfo.InvariantCulture));
                        Thread.Sleep(50); // no need to do this more often than 20 times per second
                    }
                    while (!PlaybackReady && graphBuilder != null && !BufferingStopped);
                }
                else
                {
                    DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                    PercentageBuffered = 100.0f; // no progress reporing possible
                    GUIPropertyManager.SetProperty("#TV.Record.percent3", PercentageBuffered.ToString());
                    PlaybackReady = true;
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                Log.Instance.Warn(ex.ToString());
            }
            finally
            {
                if (sourceFilter != null)
                {
                    if (!PlaybackReady)
                    {
						Log.Instance.Info("Buffering was aborted.");
                        if (sourceFilter is IAMOpenProgress) ((IAMOpenProgress)sourceFilter).AbortOperation();
                    }
                    DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                }
            }

            return PlaybackReady;
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
                DirectShowUtil.EnableDeInterlace(graphBuilder);

                if (Vmr9 == null || !Vmr9.IsVMR9Connected)
                {
                    Log.Instance.Error("OnlineVideosPlayer: Failed to render file -> No video rendere connected");
                    mediaCtrl = null;
                    Cleanup();
                    return false;
                }

                this.Vmr9.SetDeinterlaceMode();

                // now set VMR9 to Active
                GUIGraphicsContext.Vmr9Active = true;
                
                // set fields for playback                
                m_iVideoWidth = Vmr9.VideoWidth;
                m_iVideoHeight = Vmr9.VideoHeight;
                
                Vmr9.SetDeinterlaceMode();
                return true;
            }
            catch (Exception ex)
            {
                Error.SetError("Unable to play movie", "Unable build graph for VMR9");
                Log.Instance.Error("OnlineVideosPlayer:exception while creating DShow graph {0} {1}", ex.Message, ex.StackTrace);
                return false;
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
            Log.Instance.Info("OnlineVideosPlayer: Play '{0}'", strFile);

            m_bStarted = false;
            if (!GetInterfaces())
            {
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }

#if !MP11
            ISubEngine engine = SubEngine.GetInstance(true);
            if (!engine.LoadSubtitles(graphBuilder, string.IsNullOrEmpty(SubtitleFile) ? m_strCurrentFile : SubtitleFile))
            {
                SubEngine.engine = new SubEngine.DummyEngine();
            }
            else
            {
                engine.Enable = true;
            }

            IPostProcessingEngine postengine = PostProcessingEngine.GetInstance(true);
            if (!postengine.LoadPostProcessing(graphBuilder))
            {
              PostProcessingEngine.engine = new PostProcessingEngine.DummyEngine();
            }
#else
            if (!string.IsNullOrEmpty(SubtitleFile))
            {
                MediaPortal.Player.Subtitles.ISubEngine engine = MediaPortal.Player.Subtitles.SubEngine.GetInstance();
                if (engine != null)
                {
                    engine.Enable = engine.LoadSubtitles(graphBuilder, SubtitleFile);
                }
            }
#endif

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

            DirectShowUtil.SetARMode(graphBuilder, AspectRatioMode.Stretched);

            try
            {
                hr = mediaCtrl.Run();
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception error)
            {
				Log.Instance.Error("OnlineVideosPlayer: Unable to play with reason: {0}", error.Message);
            }
            if (hr < 0)
            {
                Error.SetError("Unable to play movie", "Unable to start movie");
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }
            if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
            msg.Label = strFile;
            GUIWindowManager.SendThreadMessage(msg);
            m_state = PlayState.Playing;
            m_iPositionX = GUIGraphicsContext.VideoWindow.X;
            m_iPositionY = GUIGraphicsContext.VideoWindow.Y;
            m_iWidth = GUIGraphicsContext.VideoWindow.Width;
            m_iHeight = GUIGraphicsContext.VideoWindow.Height;
            m_ar = GUIGraphicsContext.ARType;
            _updateNeeded = true;
            SetVideoWindow();
            mediaPos.get_Duration(out m_dDuration);
            Log.Instance.Info("OnlineVideosPlayer: Duration {0} sec", m_dDuration.ToString("F"));
            AnalyseStreams();
#if !MP11
            SelectSubtitles();
            SelectAudioLanguage();
#endif
            OnInitialized();

            return true;
        }

        public override void Stop()
        {
            Log.Instance.Info("OnlineVideosPlayer: Stop");
            m_strCurrentFile = "";
            CloseInterfaces();
            m_state = PlayState.Init;
            GUIGraphicsContext.IsPlaying = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            GUIPropertyManager.SetProperty("#TV.Record.percent3", 0.0f.ToString());
        }

        #region OVSPLayer Member

        public bool GoFullscreen { get; set; }
        public string SubtitleFile { get; set; }

        #endregion
    }
}
