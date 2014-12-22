using System;
using System.Threading;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using MediaPortal.Player.Subtitles;
using MediaPortal.Player.PostProcessing;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace OnlineVideos.MediaPortal1.Player
{
    public class OnlineVideosPlayer : VideoPlayerVMR9, OVSPLayer
    {
        #region Refeshrate Adaption

        [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        static extern double EVRGetVideoFPS(int fpsSource);

        [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        static extern void EVRUpdateDisplayFPS();

        string cacheFile = null;
        bool refreshRateAdapted = false;
        
        void AdaptRefreshRateFromCacheFile()
        {
            if (!PluginConfiguration.Instance.AllowRefreshRateChange)
            {
                refreshRateAdapted = true;
                return;
            }

            if (!string.IsNullOrEmpty(cacheFile))
            {
                try
                {
                    MediaInfo mi = new MediaInfo();
                    int hr = mi.Open(cacheFile);
                    double framerate;
                    double.TryParse(mi.Get(StreamKind.Video, 0, "FrameRate"), System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.NumberFormatInfo() { NumberDecimalSeparator = "." }, out framerate);
                    if (framerate > 1)
                    {
                        Log.Instance.Info("OnlineVideosPlayer got {0} FPS from MediaInfo", framerate);
                        double matchedFps = RefreshRateHelper.MatchConfiguredFPS(framerate);
                        if (matchedFps != default(double))
                        {
                            refreshRateAdapted = true;
                            RefreshRateHelper.ChangeRefreshRateToMatchedFps(matchedFps, cacheFile);
                            try
                            {
								if (GUIGraphicsContext.IsEvr)
									EVRUpdateDisplayFPS();
                            }
                            catch (EntryPointNotFoundException)
                            {
                                Log.Instance.Warn("OnlineVideosPlayer: Your version of dshowhelper.dll does not support FPS updating.");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Warn("OnlineVideosPlayer: Exception trying update refresh rate fo EVR: {0}", ex.ToString());
                            }                            
                        }
                        else
                        {
                            Log.Instance.Info("No matching configured FPS found - skipping RefreshRate Adaption from Cache File");
                        }
                    }
                    else
                    {
                        Log.Instance.Info("OnlineVideosPlayer got no FPS from MediaInfo");
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("OnlineVideosPlayer: Exception trying refresh rate change from cache file: {0}", ex.ToString());
                }
            }
            else
            {
                Log.Instance.Info("OnlineVideosPlayer: No cache file, skipping FPS detection via MediaInfo");
            }
        }

        void AdaptRefreshRateFromVideoRenderer()
        {
			if (GUIGraphicsContext.IsEvr)
			{
				if (!refreshRateAdapted && m_state == PlayState.Playing)
				{
					try
					{
						if (!PluginConfiguration.Instance.AllowRefreshRateChange)
						{
							refreshRateAdapted = true;
							return;
						}

						double fps = EVRGetVideoFPS(0);
						if (fps > 1)
						{
							refreshRateAdapted = true;
							Log.Instance.Info("OnlineVideosPlayer got {0} FPS from dshowhelper.dll after {1} sec", fps, CurrentPosition);
							double matchedFps = RefreshRateHelper.MatchConfiguredFPS(fps);
							if (matchedFps != default(double))
							{
								RefreshRateHelper.ChangeRefreshRateToMatchedFps(matchedFps, m_strCurrentFile);
								EVRUpdateDisplayFPS();
							}
							else
							{
								Log.Instance.Info("No matching configured FPS found - skipping RefreshRate Adaption");
							}
						}
					}
					catch (EntryPointNotFoundException)
					{
						Log.Instance.Warn("OnlineVideosPlayer: Your version of dshowhelper.dll does not support FPS reporting.");
						refreshRateAdapted = true;
					}
					catch (Exception ex)
					{
						Log.Instance.Warn("OnlineVideosPlayer: Exception trying refresh rate change while playing : {0}", ex.ToString());
						refreshRateAdapted = true;
					}
				}
			}
        }

        #endregion

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

		public override string CurrentFile // hack to get around the MP 1.3 Alpha bug with non http URLs
		{
			get { return "http://localhost/OnlineVideo.mp4"; }
		}

        protected override bool GetInterfaces()
        {
            if (graphBuilder != null) // graph was already started and playback file buffered
                return FinishPreparedGraph();
            else
                return base.GetInterfaces();
        }

        public override void Process()
        {
            if ((DateTime.Now - lastProgressCheck).TotalMilliseconds > 100) // check progress at maximum 10 times per second
            {
                lastProgressCheck = DateTime.Now;
                if (percentageBuffered >= 100.0f) // already buffered 100%, simply set the Property
                {
                    GUIPropertyManager.SetProperty("#TV.Record.percent3", percentageBuffered.ToString());
                }
                else
                {
					if (graphBuilder != null && GetSourceFilterName(m_strCurrentFile) == OnlineVideos.MPUrlSourceFilter.V2.Downloader.FilterName) // only when progress reporting is possible
                    {
                        IBaseFilter sourceFilter = null;
                        try
                        {
                            int result = graphBuilder.FindFilterByName(OnlineVideos.MPUrlSourceFilter.V2.Downloader .FilterName, out sourceFilter);
                            if (result == 0)
                            {
                                long total = 0, current = 0;
                                ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                                percentageBuffered = (float)current / (float)total * 100.0f;
                                GUIPropertyManager.SetProperty("#TV.Record.percent3", percentageBuffered.ToString());
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
                }
                AdaptRefreshRateFromVideoRenderer();
            }
            base.Process();
        }

        float percentageBuffered;
        DateTime lastProgressCheck = DateTime.MinValue;

        public bool BufferingStopped { get; protected set; }
        public void StopBuffering() { BufferingStopped = true; }

        protected bool skipBuffering = false;
        public void SkipBuffering() { skipBuffering = true; }

        public static string GetSourceFilterName(string videoUrl)
        {
            string sourceFilterName = string.Empty;
            Uri uri = new Uri(videoUrl);
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
            switch (protocol)
            {
                case "http":
                case "rtmp":
                    sourceFilterName = OnlineVideos.MPUrlSourceFilter.V2.Downloader.FilterName;
                    break;
                case "sop":
                    sourceFilterName = "SopCast ASF Splitter";
                    break;
                case "mms":
                    sourceFilterName = "WM ASF Reader";
                    break;
            }
            return sourceFilterName;
        }

        /// <summary>
        /// If the url to be played can be buffered before starting playback, this function
        /// starts building a graph by adding the preferred video and audio render to it.
        /// This needs to be called on the MpMain Thread.
        /// </summary>
        /// <returns>true, if the url can be buffered (a graph was started), false if it can't be and null if an error occured building the graph</returns>
        public bool? PrepareGraph()
        {
			string sourceFilterName = GetSourceFilterName(m_strCurrentFile);

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
					if (sourceFilterName == OnlineVideos.MPUrlSourceFilter.V2.Downloader.FilterName)
					{
						sourceFilter = FilterFromFile.LoadFilterFromDll("MPUrlSourceSplitter\\MPUrlSourceSplitter.ax", new Guid(OnlineVideos.MPUrlSourceFilter.V2.Downloader.FilterCLSID), true);
						if (sourceFilter != null)
							Marshal.ThrowExceptionForHR(graphBuilder.AddFilter(sourceFilter, OnlineVideos.MPUrlSourceFilter.V2.Downloader.FilterName));
					}
					if (sourceFilter == null)
					{
						sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, sourceFilterName);
					}
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
        public bool BufferFile(Sites.SiteUtilBase siteUtil)
        {
            Thread renderPinsThread = null;
            VideoRendererStatistics.VideoState = VideoRendererStatistics.State.VideoPresent; // prevents the BlackRectangle on first time playback
            bool PlaybackReady = false;
            IBaseFilter sourceFilter = null;
			string sourceFilterName = null;
            try
            {
                sourceFilterName = GetSourceFilterName(m_strCurrentFile);

                int result = graphBuilder.FindFilterByName(sourceFilterName, out sourceFilter);
                if (result != 0)
                {
                    string errorText = DirectShowLib.DsError.GetErrorText(result);
                    if (errorText != null) errorText = errorText.Trim();
                    Log.Instance.Warn("BufferFile : FindFilterByName returned '{0}'{1}", "0x" + result.ToString("X8"), !string.IsNullOrEmpty(errorText) ? " : (" + errorText + ")" : "");
                    return false;
                }

                OnlineVideos.MPUrlSourceFilter.V2.IFilterState filterState = sourceFilter as OnlineVideos.MPUrlSourceFilter.V2.IFilterState;
                OnlineVideos.MPUrlSourceFilter.V2.IFilterStateEx filterStateEx = sourceFilter as OnlineVideos.MPUrlSourceFilter.V2.IFilterStateEx;

                if (filterStateEx != null)
                {
                    // MediaPortal IPTV filter and url source splitter
                    Log.Instance.Info("BufferFile : using 'MediaPortal IPTV filter and url source splitter' as source filter");

                    String url = OnlineVideos.MPUrlSourceFilter.V2.UrlBuilder.GetFilterUrl(sourceFilter, siteUtil, m_strCurrentFile);

                    Log.Instance.Info("BufferFile : loading url: '{0}'", url);
                    result = filterStateEx.LoadAsync(url);

                    if (result < 0)
                    {
                        throw new OnlineVideosException(FilterError.ErrorDescription(filterStateEx, result));
                    }

                    while (!this.BufferingStopped)
                    {
                        Boolean opened = false;

                        result = filterStateEx.IsStreamOpened(out opened);

                        if (result < 0)
                        {
                            throw new OnlineVideosException(FilterError.ErrorDescription(filterStateEx, result));
                        }

                        if (opened)
                        {
                            break;
                        }

                        Thread.Sleep(1);
                    }

                    // buffer before starting playback
                    bool filterConnected = false;
                    bool filterIsReadyToConnect = false;
                    percentageBuffered = 0.0f;
                    long total = 0, current = 0, last = 0;

                    while (!PlaybackReady && graphBuilder != null && !BufferingStopped)
                    {
                        result = ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                        if ((result != 0) && (result != 0x00040260))
                        {
                            // 0x00040260 - VFW_S_ESTIMATED - correct state, but value is estimated
                            throw new OnlineVideosException(FilterError.ErrorDescription(filterStateEx, result));
                        }

                        result = filterStateEx.IsFilterReadyToConnectPins(out filterIsReadyToConnect);
                        if (result != 0)
                        {
                            throw new OnlineVideosException(FilterError.ErrorDescription(filterStateEx, result));
                        }

                        percentageBuffered = (float)current / (float)total * 100.0f;
                        // after configured percentage has been buffered, connect the graph

                        if (!filterConnected && (percentageBuffered >= PluginConfiguration.Instance.playbuffer || skipBuffering))
                        {
                            if (filterIsReadyToConnect)
                            {
                                result = filterStateEx.GetCacheFileName(out cacheFile);
                                if (result != 0)
                                {
                                    throw new OnlineVideosException(FilterError.ErrorDescription(filterStateEx, result));
                                }

                                if (skipBuffering) Log.Instance.Debug("Buffering skipped at {0}%", percentageBuffered);
                                filterConnected = true;
                                renderPinsThread = new Thread(delegate()
                                {
                                    try
                                    {
                                        Log.Instance.Debug("BufferFile : Rendering unconnected output pins of source filter ...");
                                        // add audio and video filter from MP Movie Codec setting section
                                        AddPreferredFilters(graphBuilder, sourceFilter);
                                        // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
                                        DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                                        Log.Instance.Debug("BufferFile : Playback Ready.");
                                        PlaybackReady = true;
                                    }
                                    catch (ThreadAbortException)
                                    {
                                        Thread.ResetAbort();
                                        Log.Instance.Info("RenderUnconnectedOutputPins foribly aborted.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Warn(ex.Message);
                                        StopBuffering();
                                    }
                                }) { IsBackground = true, Name = "OVGraph" };
                                renderPinsThread.Start();
                            }
                        }

                        // log every percent
                        if (current > last && current - last >= (double)total * 0.01)
                        {
                            Log.Instance.Debug("Buffering: {0}/{1} KB ({2}%)", current / 1024, total / 1024, (int)percentageBuffered);
                            last = current;
                        }
                        // set the percentage to a gui property, formatted according to percentage, so the user knows very early if anything is buffering                   
                        string formatString = "###";
                        if (percentageBuffered == 0f) formatString = "0.0";
                        else if (percentageBuffered < 1f) formatString = ".00";
                        else if (percentageBuffered < 10f) formatString = "0.0";
                        else if (percentageBuffered < 100f) formatString = "##";
                        GUIPropertyManager.SetProperty("#OnlineVideos.buffered", percentageBuffered.ToString(formatString, System.Globalization.CultureInfo.InvariantCulture));
                        Thread.Sleep(50); // no need to do this more often than 20 times per second
                    }
                }
                else
                {
                    String url = OnlineVideos.MPUrlSourceFilter.V2.UrlBuilder.GetFilterUrl(sourceFilter, siteUtil, m_strCurrentFile);

                    Marshal.ThrowExceptionForHR(((IFileSourceFilter)sourceFilter).Load(url, null));

                    Log.Instance.Info("BufferFile : using unknown filter as source filter");

                    if (sourceFilter is IAMOpenProgress && !url.Contains("live=true") && !url.Contains("RtmpLive=1"))
                    {
                        // buffer before starting playback
                        bool filterConnected = false;
                        percentageBuffered = 0.0f;
                        long total = 0, current = 0, last = 0;
                        do
                        {
                            result = ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                            Marshal.ThrowExceptionForHR(result);

                            percentageBuffered = (float)current / (float)total * 100.0f;
                            // after configured percentage has been buffered, connect the graph
                            if (!filterConnected && (percentageBuffered >= PluginConfiguration.Instance.playbuffer || skipBuffering))
                            {
                                //cacheFile = filterState.GetCacheFileName();
                                if (skipBuffering) Log.Instance.Debug("Buffering skipped at {0}%", percentageBuffered);
                                filterConnected = true;
                                renderPinsThread = new Thread(delegate()
                                {
                                    try
                                    {
                                        Log.Instance.Debug("BufferFile : Rendering unconnected output pins of source filter ...");
                                        // add audio and video filter from MP Movie Codec setting section
                                        AddPreferredFilters(graphBuilder, sourceFilter);
                                        // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
                                        DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                                        Log.Instance.Debug("BufferFile : Playback Ready.");
                                        PlaybackReady = true;
                                    }
                                    catch (ThreadAbortException)
                                    {
                                        Thread.ResetAbort();
                                        Log.Instance.Info("RenderUnconnectedOutputPins foribly aborted.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Warn(ex.Message);
                                        StopBuffering();
                                    }
                                }) { IsBackground = true, Name = "OVGraph" };
                                renderPinsThread.Start();
                            }
                            // log every percent
                            if (current > last && current - last >= (double)total * 0.01)
                            {
                                Log.Instance.Debug("Buffering: {0}/{1} KB ({2}%)", current / 1024, total / 1024, (int)percentageBuffered);
                                last = current;
                            }
                            // set the percentage to a gui property, formatted according to percentage, so the user knows very early if anything is buffering                   
                            string formatString = "###";
                            if (percentageBuffered == 0f) formatString = "0.0";
                            else if (percentageBuffered < 1f) formatString = ".00";
                            else if (percentageBuffered < 10f) formatString = "0.0";
                            else if (percentageBuffered < 100f) formatString = "##";
                            GUIPropertyManager.SetProperty("#OnlineVideos.buffered", percentageBuffered.ToString(formatString, System.Globalization.CultureInfo.InvariantCulture));
                            Thread.Sleep(50); // no need to do this more often than 20 times per second
                        }
                        while (!PlaybackReady && graphBuilder != null && !BufferingStopped);
                    }
                    else
                    {
                        // add audio and video filter from MP Movie Codec setting section
                        AddPreferredFilters(graphBuilder, sourceFilter);
                        // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
                        DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                        percentageBuffered = 100.0f; // no progress reporting possible
                        GUIPropertyManager.SetProperty("#TV.Record.percent3", percentageBuffered.ToString());
                        PlaybackReady = true;
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (OnlineVideosException)
            {
                throw;
            }
            catch (COMException comEx)
            {
                Log.Instance.Warn(comEx.ToString());

                string errorText = DirectShowLib.DsError.GetErrorText(comEx.ErrorCode);
                if (errorText != null) errorText = errorText.Trim();
                if (!string.IsNullOrEmpty(errorText))
                {
                    throw new OnlineVideosException(errorText);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warn(ex.ToString());
            }
            finally
            {
                if (sourceFilter != null)
                {
                    // the render pin thread was already started and is still runnning
                    if (renderPinsThread != null && (renderPinsThread.ThreadState & ThreadState.Stopped) == 0)
                    {
                        // buffering was stopped by the user -> abort the thread
                        if (BufferingStopped) renderPinsThread.Abort();
                    }

                    // playback is not ready but the source filter is already downloading -> abort the operation
                    if (!PlaybackReady)
                    {
                        Log.Instance.Info("Buffering was aborted.");
                        if (sourceFilter is IAMOpenProgress) ((IAMOpenProgress)sourceFilter).AbortOperation();
                        Thread.Sleep(100); // give it some time
                        int result = graphBuilder.RemoveFilter(sourceFilter); // remove the filter from the graph to prevent lockup later in Dispose
                    }

                    // release the COM pointer that we created
                    DirectShowUtil.ReleaseComObject(sourceFilter);
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
                    Log.Instance.Warn("OnlineVideosPlayer: Failed to render file -> No video renderer connected");
                    mediaCtrl = null;
                    Cleanup();
                    return false;
                }

                try
                {
                    // remove filter that are not used from the graph
                    DirectShowUtil.RemoveUnusedFiltersFromGraph(graphBuilder);
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error during RemoveUnusedFiltersFromGraph: {0}", ex.ToString());
                }

                if (Log.Instance.LogLevel < log4net.Core.Level.Debug)
                {
					string sourceFilterName = GetSourceFilterName(m_strCurrentFile);
                    if (!string.IsNullOrEmpty(sourceFilterName))
                    {
                        IBaseFilter sourceFilter;
                        if (graphBuilder.FindFilterByName(sourceFilterName, out sourceFilter) == 0 && sourceFilter != null)
                        {
                            LogOutputPinsConnectionRecursive(sourceFilter);
                        }
                        if (sourceFilter != null) DirectShowUtil.ReleaseComObject(sourceFilter);
                    }
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
			if (strFile != "http://localhost/OnlineVideo.mp4") m_strCurrentFile = strFile; // hack to get around the MP 1.3 Alpha bug with non http URLs
            m_bFullScreen = true;
            m_ar = GUIGraphicsContext.ARType;
            VideoRendererStatistics.VideoState = VideoRendererStatistics.State.VideoPresent;
            _updateNeeded = true;
			Log.Instance.Info("OnlineVideosPlayer: Play '{0}'", m_strCurrentFile);

            m_bStarted = false;
            if (!GetInterfaces())
            {
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }

            // if we are playing a local file set the cache file so refresh rate adaption can happen
			Uri uri = new Uri(m_strCurrentFile);
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
			if (protocol == "file") cacheFile = m_strCurrentFile;

            AdaptRefreshRateFromCacheFile();

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
            AnalyseStreams();
            SelectSubtitles();
            SelectAudioLanguage();
            OnInitialized();

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
                if (hr == 1) // S_FALSE from IMediaControl::Run means: The graph is preparing to run, but some filters have not completed the transition to a running state.
                {
                    // wait max. 20 seconds for the graph to transition to the running state
                    DateTime startTime = DateTime.Now;
                    FilterState filterState;
                    do
                    {
                        Thread.Sleep(100);
                        hr = mediaCtrl.GetState(100, out filterState); // check with timeout max. 10 times a second if the state changed
                    }
                    while ((hr != 0) && ((DateTime.Now - startTime).TotalSeconds <= 20));
                    if (hr != 0) // S_OK
                    {
                        DsError.ThrowExceptionForHR(hr);
                        throw new Exception(string.Format("IMediaControl.GetState after 20 seconds: 0x{0} - '{1}'", hr.ToString("X8"), DsError.GetErrorText(hr)));
                    }
                }
            }
            catch (Exception error)
            {
                Log.Instance.Warn("OnlineVideosPlayer: Unable to play with reason: {0}", error.Message);
            }
            if (hr != 0) // S_OK
            {
                Error.SetError("Unable to play movie", "Unable to start movie");
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }

            if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
            msg.Label = CurrentFile;
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
		public string PlaybackUrl { get { return m_strCurrentFile; } }

        #endregion

        public static void AddPreferredFilters(IGraphBuilder graphBuilder, IBaseFilter sourceFilter)
        {
            using (Settings xmlreader = new MPSettings())
            {
                bool autodecodersettings = xmlreader.GetValueAsBool("movieplayer", "autodecodersettings", false);

                if (!autodecodersettings) // the user has not chosen automatic graph building by merits
                {
                    // bool vc1ICodec,vc1Codec,xvidCodec = false; - will come later
                    bool aacCodec = false;
                    bool h264Codec = false;

                    // check the output pins of the splitter for known media types
                    IEnumPins pinEnum = null;
                    if (sourceFilter.EnumPins(out pinEnum) == 0)
                    {
                        int fetched = 0;
                        IPin[] pins = new IPin[1];
                        while (pinEnum.Next(1, pins, out fetched) == 0 && fetched > 0)
                        {
                            IPin pin = pins[0];
                            PinDirection pinDirection;
                            if (pin.QueryDirection(out pinDirection) == 0 && pinDirection == PinDirection.Output)
                            {
                                IEnumMediaTypes enumMediaTypesVideo = null;
                                if (pin.EnumMediaTypes(out enumMediaTypesVideo) == 0)
                                {
                                    AMMediaType[] mediaTypes = new AMMediaType[1];
                                    int typesFetched;
                                    while (enumMediaTypesVideo.Next(1, mediaTypes, out typesFetched) == 0 && typesFetched > 0)
                                    {
                                        if (mediaTypes[0].majorType == MediaType.Video &&
                                            (mediaTypes[0].subType == MediaSubType.H264 || mediaTypes[0].subType == MEDIASUBTYPE_AVC1))
                                        {
                                            Log.Instance.Info("found H264 video on output pin");
                                            h264Codec = true;
                                        }
                                        else if (mediaTypes[0].majorType == MediaType.Audio && mediaTypes[0].subType == MediaSubType.LATMAAC)
                                        {
                                            Log.Instance.Info("found AAC audio on output pin");
                                            aacCodec = true;
                                        }
                                    }
                                    DirectShowUtil.ReleaseComObject(enumMediaTypesVideo);
                                }
                            }
                            DirectShowUtil.ReleaseComObject(pin);
                        }
                        DirectShowUtil.ReleaseComObject(pinEnum);
                    }

                    // add filters for found media types to the graph as configured in MP
                    if (h264Codec)
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "h264videocodec", "")));
                    }
                    else
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "mpeg2videocodec", "")));
                    }
                    if (aacCodec)
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "aacaudiocodec", "")));
                    }
                    else
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "mpeg2audiocodec", "")));
                    }
                }
            }
        }

        public static readonly Guid MEDIASUBTYPE_AVC1 = new Guid("31435641-0000-0010-8000-00aa00389b71");

        public static void LogOutputPinsConnectionRecursive(IBaseFilter filter, string previous = "")
        {
            bool log = true;
            IEnumPins pinEnum = null;
            if (filter.EnumPins(out pinEnum) == 0)
            {
                FilterInfo sourceFilterInfo;
                filter.QueryFilterInfo(out sourceFilterInfo);
                int fetched = 0;
                IPin[] pins = new IPin[1];
                while (pinEnum.Next(1, pins, out fetched) == 0 && fetched > 0)
                {
                    IPin pin = pins[0];
                    PinDirection pinDirection;
                    if (pin.QueryDirection(out pinDirection) == 0 && pinDirection == PinDirection.Output)
                    {
                        log = false;
                        IPin connectedPin;
                        if (pin.ConnectedTo(out connectedPin) == 0 && connectedPin != null)
                        {
                            PinInfo connectedPinInfo;
                            connectedPin.QueryPinInfo(out connectedPinInfo);
                            FilterInfo connectedFilterInfo;
                            connectedPinInfo.filter.QueryFilterInfo(out connectedFilterInfo);
                            if (previous == "") previous = sourceFilterInfo.achName;
                            DirectShowUtil.ReleaseComObject(connectedPin, 2000);
                            IBaseFilter connectedFilter;
                            if (connectedFilterInfo.pGraph.FindFilterByName(connectedFilterInfo.achName, out connectedFilter) == 0 && connectedFilter != null)
                            {
                                LogOutputPinsConnectionRecursive(connectedFilter, previous + string.Format(" --> {0}", connectedFilterInfo.achName));
                                DirectShowUtil.ReleaseComObject(connectedFilter);
                            }
                        }
                        DirectShowUtil.ReleaseComObject(pin, 2000);
                    }
                }
            }
            DirectShowUtil.ReleaseComObject(pinEnum, 2000);

            if (log) Log.Instance.Debug(previous);
        }
    }
}
