//http://wpfmediakit.codeplex.com/

#region Usings
using System;
using System.Runtime.InteropServices;
using DirectShowLib;
using OnlineVideos;
#endregion

namespace WPFMediaKit.DirectShow.MediaPlayers
{
    /// <summary>
    /// The MediaUriPlayer plays media files from a given Uri.
    /// </summary>
    public class MediaUriPlayer : MediaSeekingPlayer
    {
        /// <summary>
        /// The name of the default audio render.  This is the
        /// same on all versions of windows
        /// </summary>
        private const string DEFAULT_AUDIO_RENDERER_NAME = "Default DirectSound Device";

        /// <summary>
        /// Set the default audio renderer property backing
        /// </summary>
        private string m_audioRenderer = DEFAULT_AUDIO_RENDERER_NAME;

#if DEBUG
        /// <summary>
        /// Used to view the graph in graphedit
        /// </summary>
        private DsROTEntry m_dsRotEntry;
#endif

        /// <summary>
        /// The DirectShow graph interface.  In this example
        /// We keep reference to this so we can dispose 
        /// of it later.
        /// </summary>
        private IGraphBuilder m_graph;

        /// <summary>
        /// The media Uri
        /// </summary>
        private Uri m_sourceUri;

        /// <summary>
        /// Gets or sets the Uri source of the media
        /// </summary>
        public Uri Source
        {
            get
            {
                VerifyAccess();
                return m_sourceUri;
            }
            set
            {
                VerifyAccess();
                m_sourceUri = value;
                
                OpenSource();
            }
        }

        /// <summary>
        /// The renderer type to use when
        /// rendering video
        /// </summary>
        public VideoRendererType VideoRenderer
        {
            get;set;
        }

        /// <summary>
        /// The name of the audio renderer device
        /// </summary>
        public string AudioRenderer
        {
            get
            {
                VerifyAccess();
                return m_audioRenderer;
            }
            set
            {
                VerifyAccess();

                if(string.IsNullOrEmpty(value))
                {
                    value = DEFAULT_AUDIO_RENDERER_NAME;
                }

                m_audioRenderer = value;
            }
        }

        /// <summary>
        /// Gets or sets if the media should play in loop
        /// or if it should just stop when the media is complete
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Is ran everytime a new media event occurs on the graph
        /// </summary>
        /// <param name="code">The Event code that occured</param>
        /// <param name="lparam1">The first event parameter sent by the graph</param>
        /// <param name="lparam2">The second event parameter sent by the graph</param>
        protected override void OnMediaEvent(EventCode code, IntPtr lparam1, IntPtr lparam2)
        {
            if(Loop)
            {
                switch(code)
                {
                    case EventCode.Complete:
                        MediaPosition = 0;
                        break;
                }
            }
            else
                /* Only run the base when we don't loop
                 * otherwise the default behavior is to
                 * fire a media ended event */
                base.OnMediaEvent(code, lparam1, lparam2);
        }

        protected override void OnGraphTimerTick()
        {
            if (m_graph != null)
            {
                IBaseFilter sourceFilter = null;
                try
                {
                    int result = m_graph.FindFilterByName(OnlineVideos.MPUrlSourceFilter.Downloader.FilterName, out sourceFilter);
                    if (result == 0)
                    {
                        long total = 0, current = 0;
                        ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                        m_BufferedPercent = (float)current / (float)total * 100.0f;
                        InvokeBufferedPercentChanged();
                    }
                }
                catch (Exception ex)
                {
                    OnlineVideos.Log.Warn("Error Quering Progress: {0}", ex.Message);
                }
                finally
                {
                    if (sourceFilter != null) Marshal.ReleaseComObject(sourceFilter);
                }
            }

            base.OnGraphTimerTick();
        }

        int AddSourceFilter(Uri uri, IFilterGraph2 filterGraph, out IBaseFilter sourceFilter)
        {
            sourceFilter = null;
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
            switch (protocol)
            {
                case "http":
                case "rtmp":
                    sourceFilter = DShowNET.Helper.FilterFromFile.LoadFilterFromDll(@"MPUrlSource\MPUrlSourceSplitter.ax", new Guid(OnlineVideos.MPUrlSourceFilter.Downloader.FilterCLSID), true);
                    return filterGraph.AddFilter(sourceFilter, OnlineVideos.MPUrlSourceFilter.Downloader.FilterName);
                case "sop":
                    sourceFilter = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("{A895A82C-7335-4D6B-A811-82E9E3C4403E}"))) as IBaseFilter;
                    return filterGraph.AddFilter(sourceFilter, "SopCast ASF Splitter");
                case "mms":
                    sourceFilter = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("{187463A0-5BB7-11D3-ACBE-0080C75E246E}"))) as IBaseFilter;
                    return filterGraph.AddFilter(sourceFilter, "WM ASF Reader");
                case "file":
                    sourceFilter = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("{E436EBB5-524F-11CE-9F53-0020AF0BA770}"))) as IBaseFilter;
                    return filterGraph.AddFilter(sourceFilter, "File Source (Async.)");
            }
            return -1;
        }

        /// <summary>
        /// Opens the media by initializing the DirectShow graph
        /// </summary>
        protected virtual void OpenSource()
        {
            /* Make sure we clean up any remaining mess */
            FreeResources();

            if (m_sourceUri == null)
                return;

            string fileSource = m_sourceUri.OriginalString;

            if (string.IsNullOrEmpty(fileSource))
                return;

            try
            {
                /* Creates the GraphBuilder COM object */
                m_graph = new FilterGraphNoThread() as IGraphBuilder;

                if (m_graph == null)
                    throw new Exception("Could not create a graph");

                /* Add our prefered audio renderer */
                InsertAudioRenderer(AudioRenderer);

                IBaseFilter renderer = CreateVideoRenderer(VideoRenderer, m_graph, 2);

                var filterGraph = m_graph as IFilterGraph2;

                if (filterGraph == null)
                    throw new Exception("Could not QueryInterface for the IFilterGraph2");

                IBaseFilter sourceFilter;

                /* Have DirectShow find the correct source filter for the Uri */
                //int hr = filterGraph.AddSourceFilter(fileSource, fileSource, out sourceFilter);

                // forced manual source filter loading if DShow couldn't find a source filter by checking registry from filename (which is likely)
                int hr = AddSourceFilter(m_sourceUri, filterGraph, out sourceFilter);

                if (hr == -1 || sourceFilter == null) throw new Exception("Could not find a source filter!");
                DsError.ThrowExceptionForHR(hr);

                hr = ((IFileSourceFilter)sourceFilter).Load(fileSource, null);
                DsError.ThrowExceptionForHR(hr);

                if (!fileSource.Contains("live=true") && !fileSource.Contains("RtmpLive=1"))
                {
                    var filterState = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterStateEx;
                    if (filterState != null)
                    {
                        // wait max. 20 seconds for the filter to be ready - then try to connect anyway
                        DateTime startTime = DateTime.Now;
                        bool readyToConnect = false;
                        while (!readyToConnect && ((DateTime.Now - startTime).TotalSeconds <= 20))
                        {
                            hr = filterState.IsFilterReadyToConnectPins(out readyToConnect);
                            if (hr < 0)
                                throw new OnlineVideosException(string.Format("Error IsFilterReadyToConnectPins: {0}", hr));
                            long total = 0, current = 0;
                            ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                            m_BufferedPercent = (float)current / (float)total * 100.0f;
                            InvokeBufferedPercentChanged();
                            System.Threading.Thread.Sleep(50); // no need to do this more often than 20 times per second
                        }
                    }
                }

                /* We will want to enum all the pins on the source filter */
                IEnumPins pinEnum;

                hr = sourceFilter.EnumPins(out pinEnum);
                DsError.ThrowExceptionForHR(hr);

                IntPtr fetched = IntPtr.Zero;
                IPin[] pins = { null };

                /* Counter for how many pins successfully rendered */
                int pinsRendered = 0;

                if (VideoRenderer == VideoRendererType.VideoMixingRenderer9)
                {
                    var mixer = renderer as IVMRMixerControl9;

                    if (mixer != null )
                    {
                        VMR9MixerPrefs dwPrefs;
                        mixer.GetMixingPrefs(out dwPrefs);
                        dwPrefs &= ~VMR9MixerPrefs.RenderTargetMask;
                        dwPrefs |= VMR9MixerPrefs.RenderTargetRGB;
                        //mixer.SetMixingPrefs(dwPrefs);
                    }
                }

                AddPreferredFiltersToGraph();

                /* Loop over each pin of the source filter */
                while (pinEnum.Next(pins.Length, pins, fetched) == 0)
                {
                    if (filterGraph.RenderEx(pins[0],
                                             AMRenderExFlags.RenderToExistingRenderers,
                                             IntPtr.Zero) >= 0)
                        pinsRendered++;

                    Marshal.ReleaseComObject(pins[0]);
                }

                Marshal.ReleaseComObject(pinEnum);
                Marshal.ReleaseComObject(sourceFilter);

                if (pinsRendered == 0)
                    throw new Exception("Could not render any streams from the source Uri");

#if DEBUG
                /* Adds the GB to the ROT so we can view
                 * it in graphedit */
                m_dsRotEntry = new DsROTEntry(m_graph);
#endif
                /* Configure the graph in the base class */
                SetupFilterGraph(m_graph);

                HasVideo = true;
                /* Sets the NaturalVideoWidth/Height */
                //SetNativePixelSizes(renderer);
            }
            catch (Exception ex)
            {
                /* This exection will happen usually if the media does
                 * not exist or could not open due to not having the
                 * proper filters installed */
                FreeResources();

                /* Fire our failed event */
                InvokeMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
            }
           
            InvokeMediaOpened();
        }

        /// <summary>
        /// Inserts the audio renderer by the name of
        /// the audio renderer that is passed
        /// </summary>
        protected virtual void InsertAudioRenderer(string audioDeviceName)
        {
            if(m_graph == null)
                return;

            AddFilterByName(m_graph, FilterCategory.AudioRendererCategory, audioDeviceName);
        }

        /// <summary>
        /// Frees all unmanaged memory and resets the object back
        /// to its initial state
        /// </summary>
        protected override void FreeResources()
        {
#if DEBUG
            /* Remove us from the ROT */
            if (m_dsRotEntry != null)
            {
                m_dsRotEntry.Dispose();
                m_dsRotEntry = null;
            }
#endif

            /* We run the StopInternal() to avoid any 
             * Dispatcher VeryifyAccess() issues because
             * this may be called from the GC */
            StopInternal();

            CloseSource();

            /* Let's clean up the base 
             * class's stuff first */
            base.FreeResources();

            if(m_graph != null)
            {
                Marshal.ReleaseComObject(m_graph);
                m_graph = null;

                /* Only run the media closed if we have an
                 * initialized filter graph */
                InvokeMediaClosed(new EventArgs());
            }

            m_BufferedPercent = 0.0f;
            InvokeBufferedPercentChanged();
        }

        protected void AddPreferredFiltersToGraph()
        {
            AddFilterByName(m_graph, FilterCategory.LegacyAmFilterCategory, "LAV Audio Decoder");
            AddFilterByName(m_graph, FilterCategory.LegacyAmFilterCategory, "LAV Video Decoder");
        }

        float m_BufferedPercent;

        public event Action<float> BufferedPercentChanged;

        protected void InvokeBufferedPercentChanged()
        {
            var handler = BufferedPercentChanged;
            if (handler != null) handler(m_BufferedPercent);
        }

        protected void CloseSource()
        {
            if (m_graph != null)
            {
                IBaseFilter sourceFilter = null;
                try
                {
                    int result = m_graph.FindFilterByName(OnlineVideos.MPUrlSourceFilter.Downloader.FilterName, out sourceFilter);
                    if (result == 0 && sourceFilter != null)
                    {
                        ((IAMOpenProgress)sourceFilter).AbortOperation();
                        System.Threading.Thread.Sleep(100); // give it some time
                        m_graph.RemoveFilter(sourceFilter); // remove the filter from the graph to prevent lockup later
                    }
                }
                catch (Exception ex)
                {
                    
                }
                finally
                {
                    if (sourceFilter != null) Marshal.FinalReleaseComObject(sourceFilter);
                }
            }
        }
    }
}