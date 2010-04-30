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

        protected override bool GetInterfaces()
        {
            string currentFileLower = CurrentFile.ToLower();

            /*if( currentFileLower.Contains(".avi") && !currentFileLower.Contains(".flv"))
                return BuildGraphForDivxStream();
            else*/ if (StartGraphWithFileSourceUrl(currentFileLower))
                return BuildGraphWithFileSourceUrl();
            else if (currentFileLower.StartsWith("mms://") || currentFileLower.Contains(".asf"))
                return BuildGraphForMMS();
            else if (currentFileLower.StartsWith("rtsp://"))
                return BuildGraphForRTSP();
            else
                return base.GetInterfaces();
        }

        bool StartGraphWithFileSourceUrl(string url)
        {
            Uri uri = new Uri(url);
            if (uri.Scheme == "http")
            {
                string extension1 = Path.GetExtension(uri.LocalPath);
                string extension2 = uri.PathAndQuery.Substring(uri.PathAndQuery.Length - 4);
                if (extension1 != ".asx" && extension2 != ".asx" && 
                   (OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extension1) || 
                   OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extension2)))
                {
                    return true;
                }
            }
            return false;
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
            PercentageBuffered = 100.0d;

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

        double _PercentageBuffered = 0;
        double PercentageBuffered 
        {
            get { return _PercentageBuffered; }
            set { _PercentageBuffered = value; GUIPropertyManager.SetProperty("#TV.Record.percent3", value.ToString()); } 
        }

        Thread bufferProgressMonitorThread;
        void MonitorBufferProgress(object filter)
        {
            try
            {
                IAMOpenProgress sourceFilter = filter as IAMOpenProgress;
                if (filter == null) return;

                int result = 0;
                long total = 0, current = 0, last = 0;
                do
                {
                    result = sourceFilter.QueryProgress(out total, out current);
                    PercentageBuffered = (double)current / (double)total * 100.0f;

                    if (current - last >= (double)total * 0.01) // log every percent
                    {
                        Log.Debug("Buffering: {0}/{1} KB ({3})%", current / 1024, total / 1024, (int)PercentageBuffered);
                        last = current;
                    }

                    Thread.Sleep(50); // no need to do this more often than 20 times per second
                }
                while (current < total && graphBuilder != null);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        bool BuildGraphWithFileSourceUrl()
        {            
            try
            {
                graphBuilder = (IGraphBuilder)new FilterGraph();
                _rotEntry = new DsROTEntry((IFilterGraph)graphBuilder);

                // add the source filter manually
                IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, "File Source (URL)");

                // load the url with the source filter                
                int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
                if (result != 0) return false;
                
                // buffer before starting playback
                try
                {
                    GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering

                    bufferProgressMonitorThread = new Thread(MonitorBufferProgress) { IsBackground = true };
                    bufferProgressMonitorThread.Start(sourceFilter);
                    while (bufferProgressMonitorThread.ThreadState == ThreadState.Running && 
                           PercentageBuffered < OnlineVideoSettings.Instance.playbuffer)
                    {
                        
                        GUIWindowManager.Process(); // keep GUI responsive
                    }                    
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    GUIWaitCursor.Hide(); // hide the wait cursor
                }
                
                /*
                // switch to directx fullscreen mode
                Log.Info("OnlineVideosPlayer: Enabling DX9 exclusive mode");
                GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
                GUIWindowManager.SendMessage(msg);
                */

                // add the VMR9 in the graph
                // after enabling exclusive mode, if done first it causes MediPortal to minimize if for example the "Windows key" is pressed while playing a video
                Vmr9 = new VMR9Util();
                Vmr9.AddVMR9(graphBuilder);
                Vmr9.Enable(false);

                // add the audio renderer
                //using (Settings settings = new MPSettings()) // only available in 1.1+
                using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                    DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
                }

                // get the output pin of the source filter
                IEnumPins enumPins;
                IPin[] sourceFilterPins = new IPin[1];
                int fetched;
                result = sourceFilter.EnumPins(out enumPins);
                result = enumPins.Next(1, sourceFilterPins, out fetched);

                // connect the pin automatically
                base.graphBuilder.Render(sourceFilterPins[0]);                

                // cleanup resources
                DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
                DirectShowUtil.ReleaseComObject(enumPins);

                if (Vmr9 == null || !Vmr9.IsVMR9Connected)
                {
                    Log.Error("OnlineVideosPlayer: Failed to render file -> vmr9");
                    mediaCtrl = null;
#if !MP102
                    Cleanup();
#endif
                    return false;
                }

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
